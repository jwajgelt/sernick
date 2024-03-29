namespace sernickTest.ControlFlowGraph;

using Diagnostics;
using sernick.Ast;
using sernick.Ast.Analysis.CallGraph;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.StructProperties;
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

    [Fact]
    public void SimpleAddition()
    {
        // var a = 1;
        // var b = 2;
        // var c : Int = a + b;

        var main = Program
        (
            Var("a", 1, out var a),
            Var("b", 2, out var b),
            Var<IntType>("c", "a".Plus("b"), out var c)
        );

        var varA = Reg(new Register());
        var varB = Reg(new Register());
        var varC = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];

        var cDef = new SingleExitNode(mainEpilogue, varC.Write(varA.Value + varB.Value));
        var abDef = new SingleExitNode(cDef, new[] { varA.Write(1), varB.Write(2) });

        var mainRoot = AddPrologue(mainContext, abDef);

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
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
            Var("a", 1, out var a),
            Var("b", 2, out var b),
            If("a".Eq("b")).Then("a".Assign(3)).Else("b".Assign(4))
        );

        var varA = Reg(new Register());
        var varB = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];

        var b4 = new SingleExitNode(mainEpilogue, varB.Write(4));
        var a3 = new SingleExitNode(mainEpilogue, varA.Write(3));
        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(a3, b4, tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, new[] {
            tmpReg.Write(new BinaryOperationNode(BinaryOperation.Equal, varA.Value, varB.Value))
        });
        var abDef = new SingleExitNode(condEval, new[] { varA.Write(1), varB.Write(2) });

        var mainRoot = AddPrologue(mainContext, abDef);

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
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramN }, true);

        fContext.AddLocal(paramN);
        var fResult = Reg(new Register());

        var fCall = fContext.GenerateCall(new[] { new Constant(new RegisterValue(5)) });
        var fCallInner1 = fContext.GenerateCall(new[] { fContext.GenerateVariableRead(paramN) - 1 });
        var fCallInner2 = fContext.GenerateCall(new[] { fContext.GenerateVariableRead(paramN) - 2 });

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];

        fCall.CodeGraph[^1].NextTree = mainEpilogue;
        var retF = new SingleExitNode(fEpilogue, fResult.Write(fCallInner1.ResultLocation! + fCallInner2.ResultLocation!));
        fCallInner2.CodeGraph[^1].NextTree = retF;
        fCallInner1.CodeGraph[^1].NextTree = fCallInner2.CodeGraph[0];
        var ret1 = new SingleExitNode(fEpilogue, fResult.Write(1));

        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(ret1, fCallInner1.CodeGraph[0], tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, tmpReg.Write(fContext.GenerateVariableRead(paramN) <= 1));

        var mainRoot = AddPrologue(mainContext, fCall.CodeGraph[0]);
        var fRoot = AddPrologue(fContext, condEval);

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
            Var("x", 0, out var x),
            Loop
            (
                "x".Assign("x".Plus(1)),
                If("x".Eq(10)).Then(Break)
            )
        );

        var varX = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
        var cond = new BinaryOperationNode(BinaryOperation.Equal, varX.Value, 10);
        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(mainEpilogue, loopBlock, tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, new[] { tmpReg.Write(cond) });
        var xPlus1 = new SingleExitNode(condEval, varX.Write(varX.Value + 1));
        loopBlock.NextTree = xPlus1;
        var xDef = new SingleExitNode(loopBlock, varX.Write(0));

        var mainRoot = AddPrologue(mainContext, xDef);

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
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramX }, true);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, new IFunctionParam[] { paramY }, true);

        fContext.AddLocal(paramX);
        gContext.AddLocal(paramY);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var gCall1 = gContext.GenerateCall(new[] { fContext.GenerateVariableRead(paramX) });
        var gCall2 = gContext.GenerateCall(new[] { fContext.GenerateVariableRead(paramX) + 1 });

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];

        var gRet = new SingleExitNode(gEpilogue, gResult.Write(gContext.GenerateVariableRead(paramY) + gContext.GenerateVariableRead(paramY)));
        var fRet = new SingleExitNode(fEpilogue, fResult.Write(gCall1.ResultLocation! + gCall2.ResultLocation!));
        gCall2.CodeGraph[^1].NextTree = fRet;
        gCall1.CodeGraph[^1].NextTree = gCall2.CodeGraph[0];

        var mainRoot = AddPrologue(mainContext, mainEpilogue);
        var fRoot = AddPrologue(fContext, gCall1.CodeGraph[0]);
        var gRoot = AddPrologue(gContext, gRet);

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
                            Var<IntType>("v4", "v1".Plus("v2").Plus("v3").Plus("p4"), out var v4),
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
        var varV3 = Mem(Mem(_displayAddress + 3 * POINTER_SIZE).Value - 8);
        var varV4 = Reg(new Register());
        var varVLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var f1Context = funFactory.CreateFunction(mainContext, Ident("f1"), null, new IFunctionParam[] { paramP1 }, true);
        var f2Context = funFactory.CreateFunction(f1Context, Ident("f2"), null, new IFunctionParam[] { paramP2 }, true);
        var f3Context = funFactory.CreateFunction(f2Context, Ident("f3"), null, new IFunctionParam[] { paramP3 }, true);
        var f4Context = funFactory.CreateFunction(f3Context, Ident("f4"), null, new IFunctionParam[] { paramP4 }, true);

        f1Context.AddLocal(paramP1);
        f2Context.AddLocal(paramP2);
        f3Context.AddLocal(paramP3);
        f4Context.AddLocal(paramP4);
        f1Context.AddLocal(v1, true);
        f2Context.AddLocal(v2, true);
        f3Context.AddLocal(v3, true);
        var f1Result = Reg(new Register());
        var f2Result = Reg(new Register());
        var f3Result = Reg(new Register());
        var f4Result = Reg(new Register());

        var f1Call = f1Context.GenerateCall(new[] { new Constant(new RegisterValue(1)) });
        var f2Call = f2Context.GenerateCall(new[] { varVLocal.Value });
        var f3Call = f3Context.GenerateCall(new[] { varVLocal.Value });
        var f4Call = f4Context.GenerateCall(new[] { varVLocal.Value });
        var f2Callv3 = f2Context.GenerateCall(new[] { varV3.Value });

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var f1Epilogue = f1Context.GenerateEpilogue(f1Result.Value)[0];
        var f2Epilogue = f2Context.GenerateEpilogue(f2Result.Value)[0];
        var f3Epilogue = f3Context.GenerateEpilogue(f3Result.Value)[0];
        var f4Epilogue = f4Context.GenerateEpilogue(f4Result.Value)[0];

        var f4Ret = new SingleExitNode(f4Epilogue, f4Result.Write(f2Callv3.ResultLocation!));
        f2Callv3.CodeGraph[^1].NextTree = f4Ret;
        var v1v4 = new SingleExitNode(f2Callv3.CodeGraph[0], varV1.Write(varV4.Value));
        var v4Def = new SingleExitNode(v1v4, varV4.Write(varV1.Value + varV2.Value + varV3.Value + f4Context.GenerateVariableRead(paramP4)));

        var f3Ret = new SingleExitNode(f3Epilogue, f3Result.Write(f4Call.ResultLocation!));
        f4Call.CodeGraph[^1].NextTree = f3Ret;
        var v2Plusv3 = new SingleExitNode(f4Call.CodeGraph[0], varV2.Write(varV2.Value + varVLocal.Value));
        var v3Def = new SingleExitNode(v2Plusv3, varVLocal.Write(varV1.Value + varV2.Value + f3Context.GenerateVariableRead(paramP3)));

        var f2Ret = new SingleExitNode(f2Epilogue, f2Result.Write(f3Call.ResultLocation!));
        f3Call.CodeGraph[^1].NextTree = f2Ret;
        var v1Plusv2 = new SingleExitNode(f3Call.CodeGraph[0], varV1.Write(varV1.Value + varVLocal.Value));
        var v2Def = new SingleExitNode(v1Plusv2, varVLocal.Write(varV1.Value + f2Context.GenerateVariableRead(paramP2)));

        var f1Ret = new SingleExitNode(f1Epilogue, f1Result.Write(f2Call.ResultLocation!));
        f2Call.CodeGraph[^1].NextTree = f1Ret;
        var v1Def = new SingleExitNode(f2Call.CodeGraph[0], varVLocal.Write(f1Context.GenerateVariableRead(paramP1)));

        f1Call.CodeGraph[^1].NextTree = mainEpilogue;

        var mainRoot = AddPrologue(mainContext, f1Call.CodeGraph[0]);
        var f1Root = AddPrologue(f1Context, v1Def);
        var f2Root = AddPrologue(f2Context, v2Def);
        var f3Root = AddPrologue(f3Context, v3Def);
        var f4Root = AddPrologue(f4Context, v4Def);

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
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramX, paramY }, true);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, new IFunctionParam[] { paramZ }, true);

        fContext.AddLocal(paramX);
        fContext.AddLocal(paramY);
        gContext.AddLocal(paramZ);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var fCall = fContext.GenerateCall(new[] { new Constant(new RegisterValue(3)) });
        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];

        fCall.CodeGraph[^1].NextTree = mainEpilogue;
        var fRet = new SingleExitNode(fEpilogue, fResult.Write(fContext.GenerateVariableRead(paramX) + fContext.GenerateVariableRead(paramY) + gCall.ResultLocation!));
        gCall.CodeGraph[^1].NextTree = fRet;
        var gRet = new SingleExitNode(gEpilogue, gResult.Write(gContext.GenerateVariableRead(paramZ)));

        var mainRoot = AddPrologue(mainContext, fCall.CodeGraph[0]);
        var fRoot = AddPrologue(fContext, gCall.CodeGraph[0]);
        var gRoot = AddPrologue(gContext, gRet);

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

        var varXf = Reg(new Register());
        var varXg = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { }, true);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, new IFunctionParam[] { }, true);

        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];

        var fRet = new SingleExitNode(fEpilogue, fResult.Write(gCall.ResultLocation! + varXf.Value));
        gCall.CodeGraph[^1].NextTree = fRet;
        var x1 = new SingleExitNode(gCall.CodeGraph[0], varXf.Write(1));
        var gRet = new SingleExitNode(gEpilogue, gResult.Write(varXg.Value));
        var xxx = new SingleExitNode(gRet, varXg.Write(varXg.Value + varXg.Value));
        var x2 = new SingleExitNode(xxx, varXg.Write(2));

        var mainRoot = AddPrologue(mainContext, mainEpilogue);
        var fRoot = AddPrologue(fContext, x1);
        var gRoot = AddPrologue(gContext, x2);

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
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { }, false);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, new IFunctionParam[] { }, false);
        var hContext = funFactory.CreateFunction(fContext, Ident("h"), null, new IFunctionParam[] { }, false);
        var zContext = funFactory.CreateFunction(hContext, Ident("z"), null, new IFunctionParam[] { }, false);

        var fCallInMain = fContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var fCallInG = fContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var hCall = hContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var zCall = zContext.GenerateCall(Array.Empty<CodeTreeValueNode>());

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(null)[0];
        var gEpilogue = gContext.GenerateEpilogue(null)[0];
        var hEpilogue = hContext.GenerateEpilogue(null)[0];
        var zEpilogue = zContext.GenerateEpilogue(null)[0];

        fCallInMain.CodeGraph[^1].NextTree = mainEpilogue;
        fCallInG.CodeGraph[^1].NextTree = gEpilogue;
        gCall.CodeGraph[^1].NextTree = zEpilogue;
        hCall.CodeGraph[^1].NextTree = fEpilogue;
        zCall.CodeGraph[^1].NextTree = hEpilogue;

        var mainRoot = AddPrologue(mainContext, fCallInMain.CodeGraph[0]);
        var fRoot = AddPrologue(fContext, hCall.CodeGraph[0]);
        var gRoot = AddPrologue(gContext, fCallInG.CodeGraph[0]);
        var hRoot = AddPrologue(hContext, zCall.CodeGraph[0]);
        var zRoot = AddPrologue(zContext, gCall.CodeGraph[0]);

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
        var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramV }, false);
        var gContext = funFactory.CreateFunction(fContext, Ident("g"), null, new IFunctionParam[] { }, false);
        var hContext = funFactory.CreateFunction(fContext, Ident("h"), null, new IFunctionParam[] { }, false);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramV);

        var fCallInMain = fContext.GenerateCall(new[] { new Constant(new RegisterValue(1)) });
        var fCallInG = fContext.GenerateCall(new[] { new Constant(new RegisterValue(0)) });
        var fCallInH = fContext.GenerateCall(new[] { new Constant(new RegisterValue(1)) });
        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var hCall = hContext.GenerateCall(Array.Empty<CodeTreeValueNode>());

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(null)[0];
        var gEpilogue = gContext.GenerateEpilogue(null)[0];
        var hEpilogue = hContext.GenerateEpilogue(null)[0];

        fCallInMain.CodeGraph[^1].NextTree = mainEpilogue;
        var x1 = new SingleExitNode(fCallInMain.CodeGraph[0], varXLocal.Write(1));
        gCall.CodeGraph[^1].NextTree = fEpilogue;
        hCall.CodeGraph[^1].NextTree = fEpilogue;

        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(gCall.CodeGraph[0], hCall.CodeGraph[0], tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, tmpReg.Write(fContext.GenerateVariableRead(paramV)));

        fCallInH.CodeGraph[^1].NextTree = hEpilogue;
        var xMinus1 = new SingleExitNode(fCallInH.CodeGraph[0], varX.Write(varX.Value - 1));
        fCallInG.CodeGraph[^1].NextTree = gEpilogue;
        var xPlus1 = new SingleExitNode(fCallInG.CodeGraph[0], varX.Write(varX.Value + 1));

        var mainRoot = AddPrologue(mainContext, x1);
        var fRoot = AddPrologue(fContext, condEval);
        var gRoot = AddPrologue(gContext, xPlus1);
        var hRoot = AddPrologue(hContext, xMinus1);

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
        var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramVf }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, new IFunctionParam[] { paramVg }, true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramVf);
        gContext.AddLocal(paramVg);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var fCall = fContext.GenerateCall(new[] { varY.Value });
        var gCall = gContext.GenerateCall(new[] { varY.Value });

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
        var yPlus1 = new SingleExitNode(loopBlock, varY.Write(varY.Value + 1));

        var tmpReg = Reg(new Register());
        var cond2 = new ConditionalJumpNode(mainEpilogue, yPlus1, tmpReg.Value);
        var gCondition = new SingleExitNode(cond2, tmpReg.Write(gCall.ResultLocation!));
        gCall.CodeGraph[^1].NextTree = gCondition;
        var cond1 = new ConditionalJumpNode(mainEpilogue, gCall.CodeGraph[0], tmpReg.Value);
        var fCondition = new SingleExitNode(cond1, tmpReg.Write(fCall.ResultLocation!));
        fCall.CodeGraph[^1].NextTree = fCondition;

        loopBlock.NextTree = fCall.CodeGraph[0];
        var xy = new SingleExitNode(loopBlock, new CodeTreeNode[] { varXLocal.Write(1), varY.Write(0) });

        var gRet = new SingleExitNode(gEpilogue, gResult.Write(gContext.GenerateVariableRead(paramVg) <= varX.Value));
        var xPlus1InG = new SingleExitNode(gRet, varX.Write(varX.Value + 1));
        var fRet = new SingleExitNode(fEpilogue, fResult.Write(fContext.GenerateVariableRead(paramVf) <= 5));
        var xPlus1InF = new SingleExitNode(fRet, varX.Write(varX.Value + 1));

        var mainRoot = AddPrologue(mainContext, xy);
        var fRoot = AddPrologue(fContext, xPlus1InF);
        var gRoot = AddPrologue(gContext, xPlus1InG);

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
        var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramVf }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, new IFunctionParam[] { paramVg }, true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramVf);
        gContext.AddLocal(paramVg);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var fCall = fContext.GenerateCall(new[] { varY.Value });
        var gCall = gContext.GenerateCall(new[] { varY.Value });

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
        var yPlus1 = new SingleExitNode(loopBlock, varY.Write(varY.Value + 1));

        var tmpReg = Reg(new Register());
        var cond2 = new ConditionalJumpNode(mainEpilogue, yPlus1, tmpReg.Value);
        var gCondition = new SingleExitNode(cond2, tmpReg.Write(gCall.ResultLocation!));
        gCall.CodeGraph[^1].NextTree = gCondition;
        var cond1 = new ConditionalJumpNode(gCall.CodeGraph[0], yPlus1, tmpReg.Value);
        var fCondition = new SingleExitNode(cond1, tmpReg.Write(fCall.ResultLocation!));
        fCall.CodeGraph[^1].NextTree = fCondition;

        loopBlock.NextTree = fCall.CodeGraph[0];
        var xy = new SingleExitNode(loopBlock, new CodeTreeNode[] { varXLocal.Write(1), varY.Write(0) });

        var gRet = new SingleExitNode(gEpilogue, gResult.Write(gContext.GenerateVariableRead(paramVg) <= varX.Value));
        var xPlus1InG = new SingleExitNode(gRet, varX.Write(varX.Value + 1));
        var fRet = new SingleExitNode(fEpilogue, fResult.Write(fContext.GenerateVariableRead(paramVf) <= 5));
        var xPlus1InF = new SingleExitNode(fRet, varX.Write(varX.Value + 1));

        var mainRoot = AddPrologue(mainContext, xy);
        var fRoot = AddPrologue(fContext, xPlus1InF);
        var gRoot = AddPrologue(gContext, xPlus1InG);

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
        var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramVf }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, new IFunctionParam[] { paramVg }, true);
        var hContext = funFactory.CreateFunction(mainContext, Ident("h"), null, new IFunctionParam[] { paramVh }, true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramVf);
        gContext.AddLocal(paramVg);
        hContext.AddLocal(paramVh);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());
        var hResult = Reg(new Register());

        var fCall = fContext.GenerateCall(new[] { varY.Value });
        var gCall = gContext.GenerateCall(new[] { varY.Value });
        var hCall = hContext.GenerateCall(new[] { varY.Value });

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];
        var hEpilogue = hContext.GenerateEpilogue(hResult.Value)[0];

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
        var yPlus1 = new SingleExitNode(loopBlock, varY.Write(varY.Value + 1));

        var tmpReg = Reg(new Register());
        var cond3 = new ConditionalJumpNode(mainEpilogue, yPlus1, tmpReg.Value);
        var hCondition = new SingleExitNode(cond3, tmpReg.Write(hCall.ResultLocation!));
        hCall.CodeGraph[^1].NextTree = hCondition;
        var cond2 = new ConditionalJumpNode(mainEpilogue, hCall.CodeGraph[0], tmpReg.Value);
        var gCondition = new SingleExitNode(cond2, tmpReg.Write(gCall.ResultLocation!));
        gCall.CodeGraph[^1].NextTree = gCondition;
        var cond1 = new ConditionalJumpNode(gCall.CodeGraph[0], yPlus1, tmpReg.Value);
        var fCondition = new SingleExitNode(cond1, tmpReg.Write(fCall.ResultLocation!));
        fCall.CodeGraph[^1].NextTree = fCondition;

        loopBlock.NextTree = fCall.CodeGraph[0];
        var xy = new SingleExitNode(loopBlock, new CodeTreeNode[] { varXLocal.Write(10), varY.Write(0) });

        var hRet = new SingleExitNode(hEpilogue, hResult.Write(varX.Value <= hContext.GenerateVariableRead(paramVh)));
        var gRet = new SingleExitNode(gEpilogue, gResult.Write(gContext.GenerateVariableRead(paramVg) <= varX.Value));
        var fRet = new SingleExitNode(fEpilogue, fResult.Write(fContext.GenerateVariableRead(paramVf) <= 5));

        var mainRoot = AddPrologue(mainContext, xy);
        var fRoot = AddPrologue(fContext, fRet);
        var gRoot = AddPrologue(gContext, gRet);
        var hRoot = AddPrologue(hContext, hRet);

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
        var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, Array.Empty<IFunctionParam>(), true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, Array.Empty<IFunctionParam>(), true);

        mainContext.AddLocal(x, true);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());

        var fCall = fContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];

        // main
        var yAssign = new SingleExitNode(mainEpilogue, varY.Write(fCall.ResultLocation! + gCall.ResultLocation!));
        gCall.CodeGraph[^1].NextTree = yAssign;
        fCall.CodeGraph[^1].NextTree = gCall.CodeGraph[0];
        var xMainAssign = new SingleExitNode(fCall.CodeGraph[0], varXLocal.Write(0));

        // f
        var fRet = new SingleExitNode(fEpilogue, fResult.Write(varX.Value));
        var xFInc = new SingleExitNode(fRet, varX.Write(varX.Value + 1));

        // g
        var gRet = new SingleExitNode(gEpilogue, gResult.Write(varX.Value));

        // add prologue
        var mainRoot = AddPrologue(mainContext, xMainAssign);
        var fRoot = AddPrologue(fContext, xFInc);
        var gRoot = AddPrologue(gContext, gRet);

        Verify(main,
            new Dictionary<FunctionDefinition, CodeTreeRoot>
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
        var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
        var varY = Reg(new Register());

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

        var f1Call = f1Context.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var f2Call = f2Context.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var f3Call = f3Context.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var f4Call = f4Context.GenerateCall(Array.Empty<CodeTreeValueNode>());

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var f1Epilogue = f1Context.GenerateEpilogue(f1Result.Value)[0];
        var f2Epilogue = f2Context.GenerateEpilogue(f2Result.Value)[0];
        var f3Epilogue = f3Context.GenerateEpilogue(f3Result.Value)[0];
        var f4Epilogue = f4Context.GenerateEpilogue(f4Result.Value)[0];

        // main
        var yAssign = new SingleExitNode(mainEpilogue,
            varY.Write(f1Call.ResultLocation!
                       + f2Call.ResultLocation!
                       + f3Call.ResultLocation!
                       + f4Call.ResultLocation!)
        );
        f4Call.CodeGraph[^1].NextTree = yAssign;
        f3Call.CodeGraph[^1].NextTree = f4Call.CodeGraph[0];
        f2Call.CodeGraph[^1].NextTree = f3Call.CodeGraph[0];
        f1Call.CodeGraph[^1].NextTree = f2Call.CodeGraph[0];
        var xMainAssign = new SingleExitNode(f1Call.CodeGraph[0], varXLocal.Write(0));

        // f1
        var f1Ret = new SingleExitNode(f1Epilogue, f1Result.Write(varX.Value));
        var xF1Inc = new SingleExitNode(f1Ret, varX.Write(varX.Value + 1));

        // f2
        var f2Ret = new SingleExitNode(f2Epilogue, f2Result.Write(varX.Value));
        var xF2Inc = new SingleExitNode(f2Ret, varX.Write(varX.Value + 2));

        // f3
        var f3Ret = new SingleExitNode(f3Epilogue, f3Result.Write(varX.Value));
        var xF3Inc = new SingleExitNode(f3Ret, varX.Write(varX.Value + 3));

        // f4
        var f4Ret = new SingleExitNode(f4Epilogue, f4Result.Write(varX.Value));
        var xF4Inc = new SingleExitNode(f4Ret, varX.Write(varX.Value + 4));

        // add prologue
        var mainRoot = AddPrologue(mainContext, xMainAssign);
        var f1Root = AddPrologue(f1Context, xF1Inc);
        var f2Root = AddPrologue(f2Context, xF2Inc);
        var f3Root = AddPrologue(f3Context, xF3Inc);
        var f4Root = AddPrologue(f4Context, xF4Inc);

        Verify(main,
            new Dictionary<FunctionDefinition, CodeTreeRoot>
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
        var varXLocal = Mem(Reg(HardwareRegister.RBP).Value - 8);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory(LabelGenerator.Generate);
        var mainContext = funFactory.CreateFunction(null, Ident(""), null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Ident("f"), null, new IFunctionParam[] { paramA, paramB }, true);
        var gContext = funFactory.CreateFunction(mainContext, Ident("g"), null, Array.Empty<IFunctionParam>(), true);
        var hContext = funFactory.CreateFunction(mainContext, Ident("h"), null, Array.Empty<IFunctionParam>(), true);

        mainContext.AddLocal(x, true);
        fContext.AddLocal(paramA);
        fContext.AddLocal(paramB);
        var fResult = Reg(new Register());
        var gResult = Reg(new Register());
        var hResult = Reg(new Register());

        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var hCall = hContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var fCall = fContext.GenerateCall(new[] { gCall.ResultLocation!, hCall.ResultLocation! });

        var mainEpilogue = mainContext.GenerateEpilogue(null)[0];
        var fEpilogue = fContext.GenerateEpilogue(fResult.Value)[0];
        var gEpilogue = gContext.GenerateEpilogue(gResult.Value)[0];
        var hEpilogue = hContext.GenerateEpilogue(hResult.Value)[0];

        // main
        var yAssign = new SingleExitNode(mainEpilogue, varY.Write(fCall.ResultLocation!));
        fCall.CodeGraph[^1].NextTree = yAssign;
        hCall.CodeGraph[^1].NextTree = fCall.CodeGraph[0];
        gCall.CodeGraph[^1].NextTree = hCall.CodeGraph[0];
        var xMainAssign = new SingleExitNode(gCall.CodeGraph[0], varXLocal.Write(0));

        // f
        var fRet = new SingleExitNode(fEpilogue, fResult.Write(fContext.GenerateVariableRead(paramA) + fContext.GenerateVariableRead(paramB)));

        // g
        var gRet = new SingleExitNode(gEpilogue, gResult.Write(varX.Value));
        var xGInc = new SingleExitNode(gRet, varX.Write(varX.Value + 1));

        // h
        var hRet = new SingleExitNode(hEpilogue, hResult.Write(varX.Value));
        var xHInc = new SingleExitNode(hRet, varX.Write(varX.Value + 2));

        // add prologue
        var mainRoot = AddPrologue(mainContext, xMainAssign);
        var fRoot = AddPrologue(fContext, fRet);
        var gRoot = AddPrologue(gContext, xGInc);
        var hRoot = AddPrologue(hContext, xHInc);

        Verify(main,
            new Dictionary<FunctionDefinition, CodeTreeRoot>
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
        var typeCheckingResult = TypeChecking.CheckTypes(ast, nameResolution, diagnostics);
        var structProperties = StructPropertiesProcessor.Process(ast, nameResolution, diagnostics);
        var functionContextMap = FunctionContextMapProcessor.Process(ast, nameResolution, typeCheckingResult, structProperties, _ => null, new FunctionFactory(LabelGenerator.Generate));
        var callGraph = CallGraphBuilder.Process(ast, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);
        var functionCodeTreeMap = FunctionCodeTreeMapGenerator.Process(ast,
            // TODO: provide StructProperties here
            root => ControlFlowAnalyzer.UnravelControlFlow(root, nameResolution, functionContextMap, callGraph, variableAccessMap, typeCheckingResult, new StructProperties(), SideEffectsAnalyzer.PullOutSideEffects));

        foreach (var (fun, codeTree) in expected)
        {
            Assert.True(functionCodeTreeMap.ContainsKey(fun));
            Assert.Equal(codeTree, functionCodeTreeMap[fun], new CfgIsomorphismComparer());
        }
    }

    private static CodeTreeRoot AddPrologue(IFunctionContext context, CodeTreeRoot graphRoot)
    {
        var prologue = context.GeneratePrologue();
        prologue[^1].NextTree ??= graphRoot;
        return prologue[0];
    }
}
