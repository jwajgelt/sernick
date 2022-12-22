namespace sernickTest.ControlFlowGraph;

using Diagnostics;
using sernick.Ast;
using sernick.Ast.Analysis.CallGraph;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.TypeChecking;
using sernick.Ast.Analysis.VariableAccess;
using sernick.Ast.Nodes;
using sernick.CodeGeneration;
using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;
using static Ast.Helpers.AstNodesExtensions;
using static sernick.Compiler.PlatformConstants;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;

public class AstToCfgConversionTest
{
    private readonly CodeTreeValueNode _displayAddress = new GlobalAddress(DisplayTable.DISPLAY_TABLE_SYMBOL);

    private static CodeTreeRoot WrapInContext(IFunctionContext context, CodeTreeValueNode? valToReturn,
        Func<CodeTreeRoot, CodeTreeRoot> generateBody)
    {
        var epilogue = new SingleExitNode(null, context.GenerateEpilogue(valToReturn));
        var body = generateBody.Invoke(epilogue);
        var prologue = new SingleExitNode(body, context.GeneratePrologue());
        return prologue;
    }

    [Fact]
    public void SimpleAddition()
    {
        // var a = 1;
        // var b = 2;
        // var c : Int = a + b;

        var main = Program
        (
            Var("a", 1, out _),
            Var("b", 2, out _),
            Var<IntType>("c", "a".Plus("b"), out _)
        );

        var varA = Reg(new Register());
        var varB = Reg(new Register());
        var varC = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var cDef = new SingleExitNode(epilogue, varC.Write(varA.Value + varB.Value));
            var abDef = new SingleExitNode(cDef, new[] { varA.Write(1), varB.Write(2) });
            return abDef;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance) {
            {main, mainRoot}
        });
    }

    [Fact]
    public void SimpleConditional()
    {
        // var a = 1;
        // var b = 2;
        // if(a == b) {
        //    a = 3;    
        // }
        // else {
        //    b = 4;
        // }

        var main = Program
        (
            Var("a", 1, out _),
            Var("b", 2, out _),
            If("a".Eq("b")).Then("a".Assign(3)).Else("b".Assign(4))
        );

        var varA = Reg(new Register());
        var varB = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var b4 = new SingleExitNode(epilogue, varB.Write(4));
            var a3 = new SingleExitNode(epilogue, varA.Write(3));
            var tmpReg = Reg(new Register());
            var ifBlock = new ConditionalJumpNode(a3, b4, tmpReg.Value);
            var condEval = new SingleExitNode(ifBlock,
                new[] { tmpReg.Write(new BinaryOperationNode(BinaryOperation.Equal, varA.Value, varB.Value)) });
            var abDef = new SingleExitNode(condEval, new[] { varA.Write(1), varB.Write(2) });
            return abDef;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot}
        });
    }

    [Fact]
    public void SimpleRecursion()
    {
        // fun f(n : Int) : Int {
        //     if(n <= 1) {
        //         return 1;
        //     }
        //     return f(n-1) + f(n-2);
        // }
        // f(5);

        var main = Program
        (
            Fun<IntType>("f").Parameter<IntType>("n", out var paramN).Body
            (
                If("n".Leq(1)).Then(Return(1)),
                Return("f".Call().Argument("n".Minus(1)).Get(out _)
                    .Plus("f".Call().Argument("n".Minus(2))))
            ).Get(out var f),
            "f".Call().Argument(Literal(5))
        );

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramN }, true);

        fContext.AddLocal(paramN, false);
        var fResult = Reg(new Register());

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
        {
            var ret1 = new SingleExitNode(epilogue, fResult.Write(1));

            var (fCallInner2, result2) = fContext
                .GenerateCall(new[] { fContext.GenerateVariableRead(paramN) - 2 });
            var (fCallInner1, result1) = fContext
                .GenerateCall(new[] { fContext.GenerateVariableRead(paramN) - 1 })
                .AsSingleExit();

            var ret2 = new SingleExitNode(epilogue, fCallInner2.Append(fResult.Write(result1! + result2!)).ToList());

            fCallInner1.NextTree = ret2;

            var tmpReg = Reg(new Register());

            var ifBlock = new ConditionalJumpNode(ret1, fCallInner1, tmpReg.Value);
            var condEval = new SingleExitNode(ifBlock, tmpReg.Write(fContext.GenerateVariableRead(paramN) <= 1));
            return condEval;
        });

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var (fCall, _) = fContext.GenerateCall(new[] { new Constant(new RegisterValue(5)) })
                .AsSingleExit(epilogue);
            return fCall;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot}
        });
    }

    [Fact]
    public void SimpleLoop()
    {
        // var x = 0;
        // loop {
        //     x = x + 1;
        //     if(x == 10) {
        //         break;
        //     }
        // }
        var main = Program
        (
            Var("x", 0, out _),
            Loop
            (
                "x".Assign("x".Plus(1)),
                If("x".Eq(10)).Then(Break)
            )
        );

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var varX = Reg(new Register());

            var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            var cond = new BinaryOperationNode(BinaryOperation.Equal, varX.Value, 10);
            var tmpReg = Reg(new Register());
            var ifBlock = new ConditionalJumpNode(epilogue, loopBlock, tmpReg.Value);
            var condEval = new SingleExitNode(ifBlock, new[] { tmpReg.Write(cond) });
            var xPlus1 = new SingleExitNode(condEval, varX.Write(varX.Value + 1));
            loopBlock.NextTree = xPlus1;

            var xDef = new SingleExitNode(loopBlock, varX.Write(0));
            return xDef;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot}
        });
    }

    [Fact]
    public void SimpleInnerFunctionCall()
    {
        // fun f(x : Int) : Int {
        //     fun g(y : Int) : Int {
        //         return y + y;
        //     }
        //     return g(x) + g(x + 1);
        // }

        var main = Program
        (
            Fun<IntType>("f").Parameter<IntType>("x", out var paramX).Body
            (
                Fun<IntType>("g").Parameter<IntType>("y", out var paramY).Body
                (
                    Return("y".Plus("y"))
                ).Get(out var g),
                Return("g".Call().Argument(Value("x")).Get(out _).Plus("g".Call().Argument("x".Plus(1))))
            ).Get(out var f)
        );

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramX }, true);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, new IFunctionParam[] { paramY }, true);

        fContext.AddLocal(paramX, false);
        gContext.AddLocal(paramY, false);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue => epilogue);

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
        {
            var (gCall1, result1) = gContext
                .GenerateCall(new[] { fContext.GenerateVariableRead(paramX) })
                .AsSingleExit();
            var (gCall2, result2) = gContext
                .GenerateCall(new[] { fContext.GenerateVariableRead(paramX) + 1 });

            var ret2 = new SingleExitNode(epilogue, gCall2.Append(fResult.Write(result1! + result2!)).ToList());
            gCall1.NextTree = ret2;
            return gCall1;
        });

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue => new SingleExitNode(epilogue,
            gResult.Write(gContext.GenerateVariableRead(paramY) + gContext.GenerateVariableRead(paramY))));

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot}
        });
    }

    [Fact]
    public void DeepFunctionCall()
    {
        // fun f1(p1 : Int) : Int {
        //     var v1 : Int = p1;
        //
        //     fun f2(p2 : Int) : Int {
        //         var v2 : Int = v1 + p2;
        //         v1 = v1 + v2;
        //
        //         fun f3(p3 : Int) : Int {
        //             var v3 : Int = v1 + v2 + p3;
        //             v2 = v2 + v3;
        //  
        //             fun f4(p4 : Int) : Int {
        //                 var v4 : Int = v1 + v2 + v3 + p4;
        //                 v1 = v4;
        //         
        //                 return f2(v3);
        //             }
        //  
        //             return f4(v3);
        //         }
        //
        //         return f3(v2);
        //     }
        //
        //     return f2(v1);
        // }
        //
        // f1(1);

        var main = Program
        (
            Fun<IntType>("f1").Parameter<IntType>("p1", out var paramP1).Body
            (
                Var<IntType>("v1", Value("p1"), out var v1),

                Fun<IntType>("f2").Parameter<IntType>("p2", out var paramP2).Body
                (
                    Var<IntType>("v2", "v1".Plus("p2"), out var v2),
                    "v1".Assign("v1".Plus("v2")),

                    Fun<IntType>("f3").Parameter<IntType>("p3", out var paramP3).Body
                    (
                        Var<IntType>("v3", "v1".Plus("v2").Plus("p3"), out var v3),
                        "v2".Assign("v2".Plus("v3")),

                        Fun<IntType>("f4").Parameter<IntType>("p4", out var paramP4).Body
                        (
                            Var<IntType>("v4", "v1".Plus("v2").Plus("v3").Plus("p4"), out _),
                            "v1".Assign(Value("v4")),

                            Return("f2".Call().Argument(Value("v3")))
                        ).Get(out var f4),

                        Return("f4".Call().Argument(Value("v3")))
                    ).Get(out var f3),

                    Return("f3".Call().Argument(Value("v2")))
                ).Get(out var f2),

                Return("f2".Call().Argument(Value("v1")))
            ).Get(out var f1),
            "f1".Call().Argument(Literal(1))
        );

        var varV1 = Mem(Mem(_displayAddress + 1 * POINTER_SIZE).Value - 8);
        var varV2 = Mem(Mem(_displayAddress + 2 * POINTER_SIZE).Value - 8);
        var varVLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var f1Context = funFactory.CreateFunction(mainContext, Ident("f1"), null, new IFunctionParam[] { paramP1 }, true);
        var f2Context = funFactory.CreateFunction(f1Context, Ident("f2"), null, new IFunctionParam[] { paramP2 }, true);
        var f3Context = funFactory.CreateFunction(f2Context, Ident("f3"), null, new IFunctionParam[] { paramP3 }, true);
        var f4Context = funFactory.CreateFunction(f3Context, Ident("f4"), null, new IFunctionParam[] { paramP4 }, true);

        f1Context.AddLocal(paramP1, false);
        f2Context.AddLocal(paramP2, false);
        f3Context.AddLocal(paramP3, false);
        f4Context.AddLocal(paramP4, false);
        f1Context.AddLocal(v1, true);
        f2Context.AddLocal(v2, true);
        f3Context.AddLocal(v3, true);
        var f1Result = Reg(new Register());
        var f2Result = Reg(new Register());
        var f3Result = Reg(new Register());
        var f4Result = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var (f1Call, _) = f1Context.GenerateCall(new[] { new Constant(new RegisterValue(1)) })
                .AsSingleExit(epilogue);
            return f1Call;
        });

        var f1Root = WrapInContext(f1Context, f1Result.Value, epilogue =>
        {
            var (f2Call, result) = f2Context
                .GenerateCall(new[] { varVLocal.Value })
                .AsSingleExit();
            var f1Ret = new SingleExitNode(epilogue, f1Result.Write(result!));
            f2Call.NextTree = f1Ret;

            var v1Def = new SingleExitNode(f2Call, varVLocal.Write(f1Context.GenerateVariableRead(paramP1)));
            return v1Def;
        });

        var f2Root = WrapInContext(f2Context, f2Result.Value, epilogue =>
        {
            var (f3Call, result) = f3Context
                .GenerateCall(new[] { varVLocal.Value })
                .AsSingleExit();
            var f2Ret = new SingleExitNode(epilogue, f2Result.Write(result!));
            f3Call.NextTree = f2Ret;

            var v1Plusv2 = new SingleExitNode(f3Call, varV1.Write(varV1.Value + varVLocal.Value));
            var v2Def = new SingleExitNode(v1Plusv2, varVLocal.Write(varV1.Value + f2Context.GenerateVariableRead(paramP2)));
            return v2Def;
        });

        var f3Root = WrapInContext(f3Context, f3Result.Value, epilogue =>
        {
            var (f4Call, result) = f4Context
                .GenerateCall(new[] { varVLocal.Value })
                .AsSingleExit();
            var f3Ret = new SingleExitNode(epilogue, f3Result.Write(result!));
            f4Call.NextTree = f3Ret;

            var v2Plusv3 = new SingleExitNode(f4Call, varV2.Write(varV2.Value + varVLocal.Value));
            var v3Def = new SingleExitNode(v2Plusv3,
                varVLocal.Write(varV1.Value + varV2.Value + f3Context.GenerateVariableRead(paramP3)));
            return v3Def;
        });

        var f4Root = WrapInContext(f4Context, f4Result.Value, epilogue =>
        {
            var varV3 = Mem(Mem(_displayAddress + 3 * POINTER_SIZE).Value - 8);
            var varV4 = Reg(new Register());

            var (f2Callv3, result) = f2Context
                .GenerateCall(new[] { varV3.Value })
                .AsSingleExit();
            var f4Ret = new SingleExitNode(epilogue, f4Result.Write(result!));
            f2Callv3.NextTree = f4Ret;

            var v1v4 = new SingleExitNode(f2Callv3, varV1.Write(varV4.Value));
            var v4Def = new SingleExitNode(v1v4,
                varV4.Write(varV1.Value + varV2.Value + varV3.Value + f4Context.GenerateVariableRead(paramP4)));
            return v4Def;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f1, f1Root},
            {f2, f2Root},
            {f3, f3Root},
            {f4, f4Root}
        });
    }

    [Fact]
    public void MultiArgumentFunctionsWithDefaultArguments()
    {
        // fun f(x : Int = 1, y : Int = 2) : Int {
        //     fun g(z : Int = 3) : Int {
        //         return z;
        //     }
        //
        //     return x + y + g();
        // }
        // f(3);

        var main = Program
        (
            Fun<IntType>("f")
            .Parameter<IntType>("x", Literal(1), out var paramX)
            .Parameter<IntType>("y", Literal(2), out var paramY)
            .Body
            (
                Fun<IntType>("g").Parameter<IntType>("z", Literal(3), out var paramZ).Body
                (
                    Return(Value("z"))
                ).Get(out var g),
                Return("x".Plus("y").Plus("g".Call()))
            ).Get(out var f),
            "f".Call().Argument(Literal(3))
        );

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramX, paramY }, true);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, new IFunctionParam[] { paramZ }, true);

        fContext.AddLocal(paramX, false);
        fContext.AddLocal(paramY, false);
        gContext.AddLocal(paramZ, false);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var (fCall, _) = fContext.GenerateCall(new[] { new Constant(new RegisterValue(3)) })
                .AsSingleExit(epilogue);
            return fCall;
        });

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
        {
            var (gCall, result) = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();
            var fRet = new SingleExitNode(epilogue,
                fResult.Write(fContext.GenerateVariableRead(paramX) + fContext.GenerateVariableRead(paramY) + result!));
            gCall.NextTree = fRet;

            var ret2 = new SingleExitNode(fRet.NextTree, gCall.Operations.Concat(fRet.Operations).ToList());
            return ret2;
        });

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue =>
            new SingleExitNode(epilogue, gResult.Write(gContext.GenerateVariableRead(paramZ))));

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot}
        });
    }

    [Fact]
    public void VariableOvershadow()
    {
        // fun f() : Int {
        //     var x = 1;
        //
        //     fun g() : Int {
        //         var x = 2;
        //         x = x + x;
        //         return x;
        //     }
        //
        //     return g() + x;
        // }

        var main = Program
        (
            Fun<IntType>("f").Body
            (
                Var("x", 1),
                Fun<IntType>("g").Body
                (
                    Var("x", 2),
                    "x".Assign("x".Plus("x")),
                    Return(Value("x"))
                ).Get(out var g),
                Return("g".Call().Get(out _).Plus("x"))
            ).Get(out var f)
        );

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, Array.Empty<IFunctionParam>(), true);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, Array.Empty<IFunctionParam>(), true);

        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue => epilogue);

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
        {
            var varXf = Reg(new Register());

            var (gCall, result) = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());

            var fRet = new SingleExitNode(epilogue, gCall.Append(fResult.Write(result! + varXf.Value)).ToList());
            var x1 = new SingleExitNode(fRet, varXf.Write(1));
            return x1;
        });

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue =>
        {
            var varXg = Reg(new Register());

            var gRet = new SingleExitNode(epilogue, gResult.Write(varXg.Value));
            var xxx = new SingleExitNode(gRet, varXg.Write(varXg.Value + varXg.Value));
            var x2 = new SingleExitNode(xxx, varXg.Write(2));
            return x2;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot}
        });
    }

    [Fact]
    public void ManyInnerFunctions()
    {
        // fun f() : Unit {
        //
        //     fun g() : Unit {
        //         f();
        //     }
        //
        //     fun h() : Unit {
        //
        //         fun z() : Unit {
        //             g();
        //         }
        //
        //         z();
        //     }
        //
        //     h();
        // }
        // f();

        var main = Program
        (
            Fun<UnitType>("f").Body
            (
                Fun<UnitType>("g").Body
                (
                    "f".Call()
                ).Get(out var g),

                Fun<UnitType>("h").Body
                (
                    Fun<UnitType>("z").Body
                    (
                        "g".Call()
                    ).Get(out var z),

                    "z".Call()
                ).Get(out var h),

                "h".Call()
            ).Get(out var f),
            "f".Call()
        );

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, Array.Empty<IFunctionParam>(), false);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, Array.Empty<IFunctionParam>(), false);
        var hContext = funFactory.CreateFunction(fContext, Ident("h"), null, Array.Empty<IFunctionParam>(), false);
        var zContext = funFactory.CreateFunction(hContext, Ident("z"), null, Array.Empty<IFunctionParam>(), false);

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var (fCallInMain, _) = fContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit(epilogue);
            return fCallInMain;
        });

        var fRoot = WrapInContext(fContext, null, epilogue =>
        {
            var (hCall, _) = hContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit(epilogue);
            return hCall;
        });

        var gRoot = WrapInContext(gContext, null, epilogue =>
        {
            var (fCallInG, _) = fContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit(epilogue);
            return fCallInG;
        });

        var hRoot = WrapInContext(hContext, null, epilogue =>
        {
            var (zCall, _) = zContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit(epilogue);
            return zCall;
        });

        var zRoot = WrapInContext(zContext, null, epilogue =>
        {
            var (gCall, _) = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit(epilogue);
            return gCall;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot},
            {h, hRoot},
            {z, zRoot}
        });
    }

    [Fact]
    public void ConditionalJumps()
    {
        // var x = 1;
        //
        // fun f(v : Bool) : Unit {
        //     fun g() : Unit {
        //         x = x + 1;
        //         f(false);
        //     }
        //
        //     fun h(): Unit {
        //         x = x - 1;
        //         f(true);
        //     }
        //
        //     if(v) {
        //         g();
        //     }
        //     else {
        //         h();
        //     }
        // }
        // f(true)

        var main = Program
        (
            Var("x", 1, out var x),
            Fun<UnitType>("f").Parameter<BoolType>("v", out var paramV).Body
            (
                Fun<UnitType>("g").Body
                (
                    "x".Assign("x".Plus(1)),
                    "f".Call().Argument(Literal(false))
                ).Get(out var g),

                Fun<UnitType>("h").Body
                (
                    "x".Assign("x".Minus(1)),
                    "f".Call().Argument(Literal(true))
                ).Get(out var h),

                If(Value("v")).Then("g".Call()).Else("h".Call())

            ).Get(out var f),
            "f".Call().Argument(Literal(true))
        );

        var varX = Mem(Mem(_displayAddress + 0 * POINTER_SIZE).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramV }, false);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, Array.Empty<IFunctionParam>(), false);
        var hContext = funFactory.CreateFunction(fContext, Ident("h"), null, Array.Empty<IFunctionParam>(), false);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramV, false);

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var (fCallInMain, _) = fContext.GenerateCall(new[] { new Constant(new RegisterValue(1)) })
                .AsSingleExit(epilogue);

            var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
            var x1 = new SingleExitNode(fCallInMain, varXLocal.Write(1));
            return x1;
        });

        var fRoot = WrapInContext(fContext, null, epilogue =>
        {
            var (gCall, _) = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit(epilogue);

            var (hCall, _) = hContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit(epilogue);

            var tmpReg = Reg(new Register());

            var ifBlock = new ConditionalJumpNode(gCall, hCall, tmpReg.Value);
            var condEval = new SingleExitNode(ifBlock, tmpReg.Write(fContext.GenerateVariableRead(paramV)));
            return condEval;
        });

        var gRoot = WrapInContext(gContext, null, epilogue =>
        {
            var (fCall, _) = fContext.GenerateCall(new[] { new Constant(new RegisterValue(0)) })
                .AsSingleExit(epilogue);

            var xPlus1 = new SingleExitNode(fCall, varX.Write(varX.Value + 1));
            return xPlus1;
        });

        var hRoot = WrapInContext(hContext, null, epilogue =>
        {
            var (fCall, _) = fContext.GenerateCall(new[] { new Constant(new RegisterValue(1)) })
                .AsSingleExit(epilogue);

            var xMinus1 = new SingleExitNode(fCall, varX.Write(varX.Value - 1));
            return xMinus1;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot},
            {h, hRoot}
        });
    }

    [Fact]
    public void ShortCircuitOr()
    {
        // var x = 1;
        //
        // fun f(v : Int) : Bool {
        //     x = x + 1;
        //     return v <= 5;
        // }
        //
        // fun g(v: Int): Bool {
        //     x = x + 1;
        //     return v <= x;
        // }
        //
        // var y = 0;
        // loop {
        //     if(f(y) || g(y)) {
        //         break;
        //     }
        //     y = y + 1;
        // }

        var main = Program
        (
            Var("x", 1, out var x),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramVf).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ).Get(out var f),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramVg).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq("x"))
            ).Get(out var g),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScOr("g".Call().Argument(Value("y"))))
                    .Then(Break),
                "y".Assign("y".Plus(1))
            )
        );

        var varX = Mem(Mem(_displayAddress + 0 * POINTER_SIZE).Value - 8);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramVf }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, new IFunctionParam[] { paramVg }, true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramVf, false);
        gContext.AddLocal(paramVg, false);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
            var tmpReg = Reg(new Register());

            var (fCall, fCallResult) = fContext.GenerateCall(new[] { varY.Value })
                .AsSingleExit();
            var (gCall, gCallResult) = gContext.GenerateCall(new[] { varY.Value })
                .AsSingleExit();

            var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            var yPlus1 = new SingleExitNode(loopBlock, varY.Write(varY.Value + 1));
            var cond2 = new ConditionalJumpNode(epilogue, yPlus1, tmpReg.Value);
            var gCondition = new SingleExitNode(cond2, tmpReg.Write(gCallResult!));
            var cond1 = new ConditionalJumpNode(epilogue, gCall, tmpReg.Value);
            var fCondition = new SingleExitNode(cond1, tmpReg.Write(fCallResult!));
            gCall.NextTree = gCondition;
            fCall.NextTree = fCondition;
            loopBlock.NextTree = fCall;

            var xy = new SingleExitNode(loopBlock, new CodeTreeNode[] { varXLocal.Write(1), varY.Write(0) });
            return xy;
        });

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
        {
            var fRet = new SingleExitNode(epilogue, fResult.Write(fContext.GenerateVariableRead(paramVf) <= 5));
            var xPlus1 = new SingleExitNode(fRet, varX.Write(varX.Value + 1));
            return xPlus1;
        });

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue =>
        {
            var gRet = new SingleExitNode(epilogue, gResult.Write(gContext.GenerateVariableRead(paramVg) <= varX.Value));
            var xPlus1 = new SingleExitNode(gRet, varX.Write(varX.Value + 1));
            return xPlus1;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot}
        });
    }

    [Fact]
    public void ShortCircuitAnd()
    {
        // var x = 1;
        //
        // fun f(v : Int) : Bool {
        //     x = x + 1;
        //     return v <= 5;
        // }
        //
        // fun g(v: Int): Bool {
        //     x = x + 1;
        //     return v <= x;
        // }
        //
        // var y = 0;
        // loop {
        //     if(f(y) && g(y)) {
        //         break;
        //     }
        //     y = y + 1;
        // }

        var main = Program
        (
            Var("x", 1, out var x),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramVf).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ).Get(out var f),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramVg).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq("x"))
            ).Get(out var g),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScAnd("g".Call().Argument(Value("y"))))
                    .Then(Break),
                "y".Assign("y".Plus(1))
            )
        );

        var varX = Mem(Mem(_displayAddress + 0 * POINTER_SIZE).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramVf }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, new IFunctionParam[] { paramVg }, true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramVf, false);
        gContext.AddLocal(paramVg, false);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
            var varY = Reg(new Register());
            var tmpReg = Reg(new Register());

            var (fCall, fCallResult) = fContext.GenerateCall(new[] { varY.Value })
                .AsSingleExit();
            var (gCall, gCallResult) = gContext.GenerateCall(new[] { varY.Value })
                .AsSingleExit();

            var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            var yPlus1 = new SingleExitNode(loopBlock, varY.Write(varY.Value + 1));
            var cond1 = new ConditionalJumpNode(gCall, yPlus1, tmpReg.Value);
            var cond2 = new ConditionalJumpNode(epilogue, yPlus1, tmpReg.Value);
            var fCondition = new SingleExitNode(cond1, tmpReg.Write(fCallResult!));
            var gCondition = new SingleExitNode(cond2, tmpReg.Write(gCallResult!));
            fCall.NextTree = fCondition;
            gCall.NextTree = gCondition;
            loopBlock.NextTree = fCall;

            var xy = new SingleExitNode(loopBlock, new CodeTreeNode[] { varXLocal.Write(1), varY.Write(0) });
            return xy;
        });

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
        {
            var fRet = new SingleExitNode(epilogue, fResult.Write(fContext.GenerateVariableRead(paramVf) <= 5));
            var xPlus1 = new SingleExitNode(fRet, varX.Write(varX.Value + 1));
            return xPlus1;
        });

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue =>
        {
            var gRet = new SingleExitNode(epilogue, gResult.Write(gContext.GenerateVariableRead(paramVg) <= varX.Value));
            var xPlus1 = new SingleExitNode(gRet, varX.Write(varX.Value + 1));
            return xPlus1;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot}
        });
    }

    [Fact]
    public void ShortCircuitOperators()
    {
        // var x = 10;
        //
        // fun f(v : Int) : Bool {
        //     return v <= 5;
        // }
        //
        // fun g(v: Int): Bool {
        //     return v <= x;
        // }
        //
        // fun h(v: Int): Bool {
        //     return x <= v;
        // }
        //
        // var y = 0;
        // loop {
        //     if(f(y) && (g(y) || h(y)) {
        //         break;
        //     }
        //     y = y + 1;
        // }

        var main = Program
        (
            Var("x", 10, out var x),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramVf).Body
            (
                Return("v".Leq(5))
            ).Get(out var f),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramVg).Body
            (
                Return("v".Leq("x"))
            ).Get(out var g),
            Fun<BoolType>("h").Parameter<IntType>("v", out var paramVh).Body
            (
                Return("x".Leq("v"))
            ).Get(out var h),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScAnd
                    (
                        "g".Call().Argument(Value("y")).Get(out _).ScOr("h".Call().Argument(Value("y")))
                    ))
                    .Then(Break),
                "y".Assign("y".Plus(1))
            )
        );

        var varX = Mem(Mem(_displayAddress + 0 * POINTER_SIZE).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramVf }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, new IFunctionParam[] { paramVg }, true);
        var hContext = funFactory.CreateFunction(mainContext, Ident("h"), null, new IFunctionParam[] { paramVh }, true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramVf, false);
        gContext.AddLocal(paramVg, false);
        hContext.AddLocal(paramVh, false);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());
        var hResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
            var varY = Reg(new Register());
            var tmpReg = Reg(new Register());

            var (fCall, fCallResult) = fContext.GenerateCall(new[] { varY.Value })
                .AsSingleExit();
            var (gCall, gCallResult) = gContext.GenerateCall(new[] { varY.Value })
                .AsSingleExit();
            var (hCall, hCallResult) = hContext.GenerateCall(new[] { varY.Value })
                .AsSingleExit();

            var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            var yPlus1 = new SingleExitNode(loopBlock, varY.Write(varY.Value + 1));
            var cond3 = new ConditionalJumpNode(epilogue, yPlus1, tmpReg.Value);
            var hCondition = new SingleExitNode(cond3, tmpReg.Write(hCallResult!));
            var cond2 = new ConditionalJumpNode(epilogue, hCall, tmpReg.Value);
            var gCondition = new SingleExitNode(cond2, tmpReg.Write(gCallResult!));
            var cond1 = new ConditionalJumpNode(gCall, yPlus1, tmpReg.Value);
            var fCondition = new SingleExitNode(cond1, tmpReg.Write(fCallResult!));
            fCall.NextTree = fCondition;
            gCall.NextTree = gCondition;
            hCall.NextTree = hCondition;
            loopBlock.NextTree = fCall;

            var xy = new SingleExitNode(loopBlock, new CodeTreeNode[] { varXLocal.Write(10), varY.Write(0) });
            return xy;
        });

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
            new SingleExitNode(epilogue, fResult.Write(fContext.GenerateVariableRead(paramVf) <= 5)));

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue =>
            new SingleExitNode(epilogue, gResult.Write(gContext.GenerateVariableRead(paramVg) <= varX.Value)));

        var hRoot = WrapInContext(hContext, hResult.Value, epilogue =>
            new SingleExitNode(epilogue, hResult.Write(varX.Value <= hContext.GenerateVariableRead(paramVh))));

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, mainRoot},
            {f, fRoot},
            {g, gRoot},
            {h, hRoot}
        });
    }

    [Fact]
    public void LeftToRightEvaluationSimple()
    {
        // var x = 0;
        //
        // fun f() : Int {
        //     x = x + 1;
        //     return x;
        // }
        //
        // fun g(): Int {
        //     return x;
        // }
        //
        // var y = f() + g();

        var main = Program
        (
            Var("x", 0, out var x),
            Fun<IntType>("f").Body
            (
                "x".Assign("x".Plus(1)),
                Return(Value("x"))
            ).Get(out var f),
            Fun<IntType>("g").Body
            (
                Return(Value("x"))
            ).Get(out var g),

            Var<IntType>("y", "f".Call().Get(out _).Plus("g".Call()))
        );

        var varX = Mem(Mem(_displayAddress + 0 * POINTER_SIZE).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, Array.Empty<IFunctionParam>(), true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, Array.Empty<IFunctionParam>(), true);

        mainContext.AddLocal(x, true);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
            var varY = Reg(new Register());

            var (fCall, fCallResult) = fContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();
            var (gCall, gCallResult) = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();

            var yAssign = new SingleExitNode(epilogue, gCall.Operations
                .Append(varY.Write(fCallResult! + gCallResult!)).ToList());
            fCall.NextTree = yAssign;

            var xAssign = new SingleExitNode(fCall, varXLocal.Write(0));
            return xAssign;
        });

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
        {
            var fRet = new SingleExitNode(epilogue, fResult.Write(varX.Value));
            var xInc = new SingleExitNode(fRet, varX.Write(varX.Value + 1));
            return xInc;
        });

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue =>
            new SingleExitNode(epilogue, gResult.Write(varX.Value)));

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>
        {
            { main, mainRoot },
            { f, fRoot },
            { g, gRoot }
        });
    }

    [Fact]
    public void LeftToRightEvaluation()
    {
        // var x = 0;
        //
        // fun f1() : Int {
        //     x = x + 1;
        //     return x;
        // }
        //
        // fun f2(): Int {
        //     x = x + 2;
        //     return x;
        // }
        //
        // fun f3(): Int {
        //     x = x + 3;
        //     return x;
        // }
        //
        // fun f4(): Int {
        //     x = x + 4;
        //     return x;
        // }
        //
        // var y = f1() + f2() + f3() + f4();

        var main = Program
        (
            Var("x", 0, out var x),
            Fun<IntType>("f1").Body
            (
                "x".Assign("x".Plus(1)),
                Return(Value("x"))
            ).Get(out var f1),
            Fun<IntType>("f2").Body
            (
                "x".Assign("x".Plus(2)),
                Return(Value("x"))
            ).Get(out var f2),
            Fun<IntType>("f3").Body
            (
                "x".Assign("x".Plus(3)),
                Return(Value("x"))
            ).Get(out var f3),
            Fun<IntType>("f4").Body
            (
                "x".Assign("x".Plus(4)),
                Return(Value("x"))
            ).Get(out var f4),
            Var<IntType>("y",
                "f1".Call().Get(out _)
                    .Plus("f2".Call())
                    .Plus("f3".Call())
                    .Plus("f4".Call())
            )
        );

        var varX = Mem(Mem(_displayAddress + 0 * POINTER_SIZE).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var f1Context = funFactory.CreateFunction(mainContext, Ident("f1"), null, Array.Empty<IFunctionParam>(), true);
        var f2Context = funFactory.CreateFunction(mainContext, Ident("f2"), null, Array.Empty<IFunctionParam>(), true);
        var f3Context = funFactory.CreateFunction(mainContext, Ident("f3"), null, Array.Empty<IFunctionParam>(), true);
        var f4Context = funFactory.CreateFunction(mainContext, Ident("f4"), null, Array.Empty<IFunctionParam>(), true);

        mainContext.AddLocal(x, true);
        var f1Result = Reg(new Register());
        var f2Result = Reg(new Register());
        var f3Result = Reg(new Register());
        var f4Result = Reg(new Register());

        // add prologue
        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
            var varY = Reg(new Register());

            var (f1Call, f1CallResult) = f1Context.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();
            var (f2Call, f2CallResult) = f2Context.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();
            var (f3Call, f3CallResult) = f3Context.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();
            var (f4Call, f4CallResult) = f4Context.GenerateCall(Array.Empty<CodeTreeValueNode>());

            var yAssign = new SingleExitNode(epilogue,
                f4Call.Append(varY.Write(f1CallResult! + f2CallResult! + f3CallResult! + f4CallResult!)).ToList()
            );
            f3Call.NextTree = yAssign;
            f2Call.NextTree = f3Call;
            f1Call.NextTree = f2Call;
            var xAssign = new SingleExitNode(f1Call, varXLocal.Write(0));
            return xAssign;
        });

        var f1Root = WrapInContext(f1Context, f1Result.Value, epilogue =>
        {
            var f1Ret = new SingleExitNode(epilogue, f1Result.Write(varX.Value));
            var xInc = new SingleExitNode(f1Ret, varX.Write(varX.Value + 1));
            return xInc;
        });

        var f2Root = WrapInContext(f2Context, f2Result.Value, epilogue =>
        {
            var f2Ret = new SingleExitNode(epilogue, f2Result.Write(varX.Value));
            var xInc = new SingleExitNode(f2Ret, varX.Write(varX.Value + 2));
            return xInc;
        });

        var f3Root = WrapInContext(f3Context, f3Result.Value, epilogue =>
        {
            var f3Ret = new SingleExitNode(epilogue, f3Result.Write(varX.Value));
            var xInc = new SingleExitNode(f3Ret, varX.Write(varX.Value + 3));
            return xInc;
        });

        var f4Root = WrapInContext(f4Context, f4Result.Value, epilogue =>
        {
            var f4Ret = new SingleExitNode(epilogue, f4Result.Write(varX.Value));
            var xInc = new SingleExitNode(f4Ret, varX.Write(varX.Value + 4));
            return xInc;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>
        {
            { main, mainRoot },
            { f1, f1Root },
            { f2, f2Root },
            { f3, f3Root },
            { f4, f4Root }
        });
    }

    [Fact]
    public void LeftToRightEvaluationInArguments()
    {
        // var x = 0;
        //
        // fun f(a : Int, b : int) : Int {
        //     return a + b;
        // }
        //
        // fun g() : Int {
        //     x = x + 1;
        //     return x;
        // }
        //
        // fun h(): Int {
        //     x = x + 2;
        //     return x;
        // }
        //
        // var y = f(g(), h());

        var main = Program
        (
            Var("x", 0, out var x),
            Fun<IntType>("f")
                .Parameter<IntType>("a", out var paramA)
                .Parameter<IntType>("b", out var paramB)
                .Body
                (
                    Return("a".Plus("b"))
                ).Get(out var f),
            Fun<IntType>("g").Body
            (
                "x".Assign("x".Plus(1)),
                Return(Value("x"))
            ).Get(out var g),
            Fun<IntType>("h").Body
            (
                "x".Assign("x".Plus(2)),
                Return(Value("x"))
            ).Get(out var h),

            Var<IntType>("y", "f".Call().Argument("g".Call()).Argument("h".Call()))
        );

        var varX = Mem(Mem(_displayAddress + 0 * POINTER_SIZE).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramA, paramB }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, Array.Empty<IFunctionParam>(), true);
        var hContext = funFactory.CreateFunction(mainContext, Ident("h"), null, Array.Empty<IFunctionParam>(), true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramA, false);
        fContext.AddLocal(paramB, false);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());
        var hResult = Reg(new Register());

        var mainRoot = WrapInContext(mainContext, null, epilogue =>
        {
            var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
            var varY = Reg(new Register());

            var (gCall, gCallResult) = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();
            var (hCall, hCallResult) = hContext.GenerateCall(Array.Empty<CodeTreeValueNode>())
                .AsSingleExit();
            var (fCall, fCallResult) = fContext.GenerateCall(new[] { gCallResult!, hCallResult! })
                .AsSingleExit();

            var yAssign = new SingleExitNode(epilogue, varY.Write(fCallResult!));
            fCall.NextTree = yAssign;
            hCall.NextTree = fCall;
            gCall.NextTree = hCall;

            var xAssign = new SingleExitNode(gCall, varXLocal.Write(0));
            return xAssign;
        });

        var fRoot = WrapInContext(fContext, fResult.Value, epilogue =>
            new SingleExitNode(epilogue,
                fResult.Write(fContext.GenerateVariableRead(paramA) + fContext.GenerateVariableRead(paramB))));

        var gRoot = WrapInContext(gContext, gResult.Value, epilogue =>
        {
            var gRet = new SingleExitNode(epilogue, gResult.Write(varX.Value));
            var xInc = new SingleExitNode(gRet, varX.Write(varX.Value + 1));
            return xInc;
        });

        var hRoot = WrapInContext(hContext, hResult.Value, epilogue =>
        {
            var hRet = new SingleExitNode(epilogue, hResult.Write(varX.Value));
            var xInc = new SingleExitNode(hRet, varX.Write(varX.Value + 2));
            return xInc;
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>
        {
            { main, mainRoot },
            { f, fRoot },
            { g, gRoot },
            { h, hRoot }
        });
    }

    private static void Verify(FunctionDefinition ast, IReadOnlyDictionary<FunctionDefinition, CodeTreeRoot> expected)
    {
        var diagnostics = new FakeDiagnostics();
        var nameResolution = NameResolutionAlgorithm.Process(ast, diagnostics);
        var functionContextMap = FunctionContextMapProcessor.Process(ast, nameResolution, _ => null, new FunctionFactory(LabelGenerator.Generate));
        var callGraph = CallGraphBuilder.Process(ast, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);
        var typeCheckingResult = TypeChecking.CheckTypes(ast, nameResolution, diagnostics);
        var functionCodeTreeMap = FunctionCodeTreeMapGenerator.Process(ast,
            root => ControlFlowAnalyzer.UnravelControlFlow(root, nameResolution, functionContextMap, callGraph, variableAccessMap, typeCheckingResult, SideEffectsAnalyzer.PullOutSideEffects));

        foreach (var (fun, codeTree) in expected)
        {
            Assert.True(functionCodeTreeMap.ContainsKey(fun));
            Assert.Equal(codeTree, functionCodeTreeMap[fun], new CfgIsomorphismComparer());
        }
    }
}

internal static class AstToCfgHelper
{
    internal static (SingleExitNode root, CodeTreeValueNode? resultLocation) AsSingleExit(
        this IFunctionCaller.GenerateCallResult callResult, CodeTreeRoot? next = null)
    {
        return (new SingleExitNode(next, callResult.CodeGraph), callResult.ResultLocation);
    }
}
