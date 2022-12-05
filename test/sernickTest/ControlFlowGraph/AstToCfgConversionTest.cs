namespace sernickTest.ControlFlowGraph;

using Diagnostics;
using sernick.Ast;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.VariableAccess;
using sernick.Ast.Nodes;
using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;
using static Ast.Helpers.AstNodesExtensions;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static sernick.Compiler.PlatformConstants;

public class AstToCfgConversionTest
{
    private readonly CodeTreeValueNode _displayAddress = new GlobalAddress("display");
    private readonly CodeTreeRoot _empty = new SingleExitNode(null, Array.Empty<CodeTreeNode>());

    [Fact]
    public void SimpleAddition()
    {
        // var a = 1;
        // var b = 2;
        // var c : Int = a + b;

        var main = Program
        (
            Var("a", 1),
            Var("b", 2),
            Var<IntType>("c", "a".Plus("b"))
        );

        var varA = Reg(new Register());
        var varB = Reg(new Register());
        var varC = Reg(new Register());

        var c = new SingleExitNode(null, new[] { varC.Write(varA.Value + varB.Value) });
        var ab = new SingleExitNode(c, new[] { varA.Write(1), varB.Write(2) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, ab}
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
            Var("a", 1),
            Var("b", 2),
            If("a".Eq("b")).Then("a".Assign(3)).Else("b".Assign(4))
        );

        var varA = Reg(new Register());
        var varB = Reg(new Register());

        var b4 = new SingleExitNode(null, new[] { varB.Write(4) });
        var a3 = new SingleExitNode(null, new[] { varA.Write(3) });
        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(a3, b4, tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, new[] { 
            tmpReg.Write(new BinaryOperationNode(BinaryOperation.Equal, varA.Value, varB.Value))
        });
        var ab = new SingleExitNode(condEval, new[] { varA.Write(1), varB.Write(2) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, ab}
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

        var varN = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramN }, true);

        var fCall = fContext.GenerateCall(new[] { new Constant(new RegisterValue(5)) });
        var fCallInner1 = fContext.GenerateCall(new[] { varN.Value - 1 });
        var fCallInner2 = fContext.GenerateCall(new[] { varN.Value - 2 });

        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var retF = new SingleExitNode(null, new[] { fCallInner1.ResultLocation! + fCallInner2.ResultLocation! });
        var fCallInner2Tree = new SingleExitNode(retF, fCallInner2.CodeGraph);
        var fCallInner1Tree = new SingleExitNode(fCallInner2Tree, fCallInner1.CodeGraph);
        var ret1 = new SingleExitNode(null, new[] { new Constant(new RegisterValue(1)) });

        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(ret1, fCallInner1Tree, tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, new[] { 
            tmpReg.Write(varN.Value <= 1)
        });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, fCallTree},
            {f, ifBlock}
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
            Var("x", 0),
            Loop
            (
                "x".Assign("x".Plus(1)),
                If("x".Eq(10)).Then(Break)
            )
        );

        var varX = Reg(new Register());

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());

        var cond = new BinaryOperationNode(BinaryOperation.Equal, varX.Value, 10);
        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(null, loopBlock, tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, new[] { 
            tmpReg.Write(cond)
        });

        var xPlus1 = new SingleExitNode(ifBlock, new[] { varX.Write(varX.Value + 1) });
        loopBlock.NextTree = xPlus1;

        var x = new SingleExitNode(xPlus1, new[] { varX.Write(0) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, x}
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

        var varX = Reg(HardwareRegister.RDI);
        var varY = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramX }, true);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { paramY }, true);

        var gCall1 = gContext.GenerateCall(new[] { varX.Value - 1 });
        var gCall2 = gContext.GenerateCall(new[] { varX.Value - 2 });

        var gRet = new SingleExitNode(null, new[] { varY.Value + varY.Value });
        var fRet = new SingleExitNode(null, new[] { gCall1.ResultLocation! + gCall2.ResultLocation! });
        var gCalls = new SingleExitNode(fRet, gCall1.CodeGraph.Concat(gCall2.CodeGraph).ToList());

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, _empty},
            {f, gCalls},
            {g, gRet}
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
                Var<IntType>("v1", Value("p1")),

                Fun<IntType>("f2").Parameter<IntType>("p2", out var paramP2).Body
                (
                    Var<IntType>("v2", "v1".Plus("p2")),
                    "v1".Assign("v1".Plus("v2")),

                    Fun<IntType>("f3").Parameter<IntType>("p3", out var paramP3).Body
                    (
                        Var<IntType>("v3", "v1".Plus("v2").Plus("p3")),
                        "v2".Assign("v2".Plus("v3")),

                        Fun<IntType>("f4").Parameter<IntType>("p4", out var paramP4).Body
                        (
                            Var<IntType>("v4", "v1".Plus("v2").Plus("v3").Plus("p4")),
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

        var varV1 = Mem(Mem(_displayAddress + 1 * POINTER_SIZE).Value);
        var varV2 = Mem(Mem(_displayAddress + 2 * POINTER_SIZE).Value);
        var varV3 = Mem(Mem(_displayAddress + 3 * POINTER_SIZE).Value);
        var varV4 = Mem(Mem(_displayAddress + 4 * POINTER_SIZE).Value);
        var varP = Reg(HardwareRegister.RDI); // P1,P2,P3,P4 are always under RDI 

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var f1Context = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramP1 }, true);
        var f2Context = funFactory.CreateFunction(f1Context, new IFunctionParam[] { paramP2 }, true);
        var f3Context = funFactory.CreateFunction(f2Context, new IFunctionParam[] { paramP3 }, true);
        var f4Context = funFactory.CreateFunction(f3Context, new IFunctionParam[] { paramP4 }, true);

        var f1Call = f1Context.GenerateCall(new[] { new Constant(new RegisterValue(1)) });
        var f2Callv1 = f2Context.GenerateCall(new[] { varV1.Value });
        var f3Call = f3Context.GenerateCall(new[] { varV2.Value });
        var f4Call = f4Context.GenerateCall(new[] { varV3.Value });
        var f2Callv3 = f2Context.GenerateCall(new[] { varV3.Value });

        var f4Ret = new SingleExitNode(null, new[] { f2Callv3.ResultLocation! });
        var f2Callv3Tree = new SingleExitNode(f4Ret, f2Callv3.CodeGraph);
        var v1v4 = new SingleExitNode(f2Callv3Tree, new[] { varV1.Write(varV4.Value) });
        var v4 = new SingleExitNode(v1v4, new[] { varV4.Write(varV1.Value + varV2.Value + varV3.Value + varP.Value) });

        var f3Ret = new SingleExitNode(null, new[] { f4Call.ResultLocation! });
        var f4CallTree = new SingleExitNode(f3Ret, f4Call.CodeGraph);
        var v2Plusv3 = new SingleExitNode(f4CallTree, new[] { varV2.Write(varV2.Value + varV3.Value) });
        var v3 = new SingleExitNode(v2Plusv3, new[] { varV3.Write(varV1.Value + varV2.Value + varP.Value) });

        var f2Ret = new SingleExitNode(null, new[] { f3Call.ResultLocation! });
        var f3CallTree = new SingleExitNode(f2Ret, f3Call.CodeGraph);
        var v1Plusv2 = new SingleExitNode(f3CallTree, new[] { varV1.Write(varV1.Value + varV2.Value) });
        var v2 = new SingleExitNode(v1Plusv2, new[] { varV2.Write(varV1.Value + varP.Value) });

        var f1Ret = new SingleExitNode(null, new[] { f2Callv1.ResultLocation! });
        var f2Callv1Tree = new SingleExitNode(f1Ret, f2Callv1.CodeGraph);
        var v1 = new SingleExitNode(f2Callv1Tree, new[] { varV1.Write(varP.Value) });

        var f1CallTree = new SingleExitNode(null, f1Call.CodeGraph);

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, f1CallTree},
            {f1, v1},
            {f2, v2},
            {f3, v3},
            {f4, v4}
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

        var varX = Reg(HardwareRegister.RDI);
        var varY = Reg(HardwareRegister.RSI);
        var varZ = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramX, paramY }, true);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { paramZ }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { });

        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var fRet = new SingleExitNode(null, new[] { varX.Value + varY.Value + gCall.ResultLocation! });
        var gCallTree = new SingleExitNode(fRet, gCall.CodeGraph);
        var gRet = new SingleExitNode(null, new[] { varZ.Value });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, fCallTree},
            {f, gCallTree},
            {g, gRet}
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

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { }, true);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { }, true);

        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { });

        var fRet = new SingleExitNode(null, new[] { gCall.ResultLocation! + varXf.Value });
        var gCallTree = new SingleExitNode(fRet, gCall.CodeGraph);
        var x1 = new SingleExitNode(gCallTree, new[] { varXf.Write(1) });
        var gRet = new SingleExitNode(null, new[] { varXg.Value });
        var xxx = new SingleExitNode(gRet, new[] { varXg.Write(varXg.Value + varXg.Value) });
        var x2 = new SingleExitNode(xxx, new[] { varXg.Write(2) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, _empty},
            {f, x1},
            {g, x2}
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

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { }, false);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { }, false);
        var hContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { }, false);
        var zContext = funFactory.CreateFunction(hContext, new IFunctionParam[] { }, false);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { });
        var hCall = hContext.GenerateCall(new CodeTreeValueNode[] { });
        var zCall = zContext.GenerateCall(new CodeTreeValueNode[] { });

        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var hCallTree = new SingleExitNode(null, hCall.CodeGraph);
        var zCallTree = new SingleExitNode(null, zCall.CodeGraph);

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, fCallTree},
            {f, hCallTree},
            {g, fCallTree},
            {h, zCallTree},
            {z, gCallTree}
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
            Var("x", 1),
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

        var varX = Mem(Mem(_displayAddress).Value);
        var varV = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramV }, false);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { }, false);
        var hContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { }, false);

        var fCallInMain = fContext.GenerateCall(new CodeTreeValueNode[] { new Constant(new RegisterValue(1)) });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { });
        var hCall = hContext.GenerateCall(new CodeTreeValueNode[] { });
        var fCallInG = fContext.GenerateCall(new CodeTreeValueNode[] { new Constant(new RegisterValue(0)) });
        var fCallInH = fContext.GenerateCall(new CodeTreeValueNode[] { new Constant(new RegisterValue(1)) });

        var fCallInMainTree = new SingleExitNode(null, fCallInMain.CodeGraph);
        var x1 = new SingleExitNode(fCallInMainTree, new[] { varX.Write(1) });
        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var hCallTree = new SingleExitNode(null, hCall.CodeGraph);
        
        var tmpReg = Reg(new Register());
        var ifBlock = new ConditionalJumpNode(gCallTree, hCallTree, tmpReg.Value);
        var condEval = new SingleExitNode(ifBlock, new[] { tmpReg.Write(varV.Value) });

        var fCallInHTree = new SingleExitNode(null, fCallInH.CodeGraph);
        var xMinus1 = new SingleExitNode(fCallInHTree, new[] { varX.Write(varX.Value - 1) });
        var fCallInGTree = new SingleExitNode(null, fCallInG.CodeGraph);
        var xPlus1 = new SingleExitNode(fCallInGTree, new[] { varX.Write(varX.Value + 1) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, x1},
            {f, ifBlock},
            {g, xPlus1},
            {h, xMinus1}
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
            Var("x", 1),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramF).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ).Get(out var f),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramG).Body
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

        var varX = Mem(Mem(_displayAddress).Value);
        var varV = Reg(HardwareRegister.RDI);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramF }, true);
        var gContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramG }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { varY.Value });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { varY.Value });

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
        var yPlus1 = new SingleExitNode(loopBlock, new[] { varY.Write(varY.Value + 1) });
        var cond2 = new ConditionalJumpNode(null, yPlus1, gCall.ResultLocation!);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(null, gCallTree, fCall.ResultLocation!);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        loopBlock.NextTree = fCallTree;

        var xy = new SingleExitNode(cond1, new CodeTreeNode[] { varX.Write(1), varY.Write(0) });

        var gRet = new SingleExitNode(null, new[] { varV.Value <= varX.Value });
        var xPlus1InG = new SingleExitNode(gRet, new[] { varX.Write(varX.Value + 1) });
        var fRet = new SingleExitNode(null, new CodeTreeNode[] { varX.Write(varX.Value + 1), varV.Value <= 5 });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, xy},
            {f, fRet},
            {g, xPlus1InG}
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
            Var("x", 1),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramF).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ).Get(out var f),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramG).Body
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

        var varX = Mem(Mem(_displayAddress).Value);
        var varV = Reg(HardwareRegister.RDI);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramF }, true);
        var gContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramG }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { varY.Value });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { varY.Value });

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
        var yPlus1 = new SingleExitNode(loopBlock, new[] { varY.Write(varY.Value + 1) });
        var cond2 = new ConditionalJumpNode(null, yPlus1, gCall.ResultLocation!);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(gCallTree, yPlus1, fCall.ResultLocation!);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        loopBlock.NextTree = fCallTree;

        var xy = new SingleExitNode(cond1, new CodeTreeNode[] { varX.Write(1), varY.Write(0) });

        var gRet = new SingleExitNode(null, new[] { varV.Value <= varX.Value });
        var xPlus1InG = new SingleExitNode(gRet, new[] { varX.Write(varX.Value + 1) });
        var fRet = new SingleExitNode(null, new CodeTreeNode[] { varX.Write(varX.Value + 1), varV.Value <= 5 });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance){
            {main, xy},
            {f, fRet},
            {g, xPlus1InG}
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
            Var("x", 10),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramF).Body
            (
                Return("v".Leq(5))
            ).Get(out var f),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramG).Body
            (
                Return("v".Leq("x"))
            ).Get(out var g),
            Fun<BoolType>("h").Parameter<IntType>("v", out var paramH).Body
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

        var varX = Mem(Mem(_displayAddress).Value);
        var varV = Reg(HardwareRegister.RDI);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] { }, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramF }, true);
        var gContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramG }, true);
        var hContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramH }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { varY.Value });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { varY.Value });
        var hCall = hContext.GenerateCall(new CodeTreeValueNode[] { varY.Value });

        var loopBlock = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
        var yPlus1 = new SingleExitNode(loopBlock, new[] { varY.Write(varY.Value + 1) });
        var cond3 = new ConditionalJumpNode(null, yPlus1, hCall.ResultLocation!);
        var hCallTree = new SingleExitNode(cond3, gCall.CodeGraph);
        var cond2 = new ConditionalJumpNode(null, hCallTree, gCall.ResultLocation!);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(gCallTree, yPlus1, fCall.ResultLocation!);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        loopBlock.NextTree = fCallTree;

        var xy = new SingleExitNode(cond1, new CodeTreeNode[] { varX.Write(10), varY.Write(0) });

        var hRet = new SingleExitNode(null, new[] { varX.Value <= varV.Value });
        var gRet = new SingleExitNode(null, new[] { varV.Value <= varX.Value });
        var fRet = new SingleExitNode(null, new[] { varV.Value <= 5 });

        Verify(main,
            new Dictionary<FunctionDefinition, CodeTreeRoot>(ReferenceEqualityComparer.Instance)
            {
                { main, xy }, { f, fRet }, { g, gRet }, { h, hRet }
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
            Var("x", 0),
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

        var varX = Mem(Mem(_displayAddress).Value);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);
        var gContext = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);

        var fCall = fContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());

        // main
        var yAssign = new SingleExitNode(null, new[] { varY.Write(fCall.ResultLocation! + gCall.ResultLocation!) });
        var gEval = new SingleExitNode(yAssign, gCall.CodeGraph);
        var fEval = new SingleExitNode(gEval, fCall.CodeGraph);
        var xMainAssign = new SingleExitNode(fEval, new[] { varX.Write(0) });

        // f
        var fRet = new SingleExitNode(null, new[] { varX.Value });
        var xFInc = new SingleExitNode(fRet, new[] { varX.Write(varX.Value + 1) });

        // g
        var gRet = new SingleExitNode(null, new[] { varX.Value });

        // wrap in prologue and epilogue
        var mainRoot = WrapInContext(mainContext, xMainAssign, new[] { yAssign }, null);
        var fRoot = WrapInContext(fContext, xFInc, new[] { fRet }, fCall.ResultLocation);
        var gRoot = WrapInContext(gContext, gRet, new[] { gRet }, gCall.ResultLocation);

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
            Var("x", 0),
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

        var varX = Mem(Mem(_displayAddress).Value);
        var varY = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, Array.Empty<IFunctionParam>(), false);
        var f1Context = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);
        var f2Context = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);
        var f3Context = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);
        var f4Context = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);

        var f1Call = f1Context.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var f2Call = f2Context.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var f3Call = f3Context.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var f4Call = f4Context.GenerateCall(Array.Empty<CodeTreeValueNode>());

        // main
        var yAssign = new SingleExitNode(null, new[]
        {
            varY.Write(f1Call.ResultLocation!
                       + f2Call.ResultLocation!
                       + f3Call.ResultLocation!
                       + f4Call.ResultLocation!)
        });
        var f4Eval = new SingleExitNode(yAssign, f4Call.CodeGraph);
        var f3Eval = new SingleExitNode(f4Eval, f3Call.CodeGraph);
        var f2Eval = new SingleExitNode(f3Eval, f2Call.CodeGraph);
        var f1Eval = new SingleExitNode(f2Eval, f1Call.CodeGraph);
        var xMainAssign = new SingleExitNode(f1Eval, new[] { varX.Write(0) });

        // f1
        var f1Ret = new SingleExitNode(null, new[] { varX.Value });
        var xF1Inc = new SingleExitNode(f1Ret, new[] { varX.Write(varX.Value + 1) });

        // f2
        var f2Ret = new SingleExitNode(null, new[] { varX.Value });
        var xF2Inc = new SingleExitNode(f2Ret, new[] { varX.Write(varX.Value + 2) });

        // f3
        var f3Ret = new SingleExitNode(null, new[] { varX.Value });
        var xF3Inc = new SingleExitNode(f3Ret, new[] { varX.Write(varX.Value + 3) });

        // f4
        var f4Ret = new SingleExitNode(null, new[] { varX.Value });
        var xF4Inc = new SingleExitNode(f4Ret, new[] { varX.Write(varX.Value + 4) });

        // wrap in prologue and epilogue
        var mainRoot = WrapInContext(mainContext, xMainAssign, new[] { yAssign }, null);
        var f1Root = WrapInContext(f1Context, xF1Inc, new[] { f1Ret }, f1Call.ResultLocation);
        var f2Root = WrapInContext(f2Context, xF2Inc, new[] { f2Ret }, f2Call.ResultLocation);
        var f3Root = WrapInContext(f3Context, xF3Inc, new[] { f3Ret }, f3Call.ResultLocation);
        var f4Root = WrapInContext(f4Context, xF4Inc, new[] { f4Ret }, f4Call.ResultLocation);

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
            Var("x", 0),
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

        var varX = Mem(Mem(_displayAddress).Value);
        var varY = Reg(new Register());
        var varA = Reg(HardwareRegister.RDI);
        var varB = Reg(HardwareRegister.RSI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, Array.Empty<IFunctionParam>(), false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramA, paramB }, true);
        var gContext = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);
        var hContext = funFactory.CreateFunction(mainContext, Array.Empty<IFunctionParam>(), true);

        var gCall = gContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var hCall = hContext.GenerateCall(Array.Empty<CodeTreeValueNode>());
        var fCall = fContext.GenerateCall(new[] { gCall.ResultLocation!, hCall.ResultLocation! });

        // main
        var yAssign = new SingleExitNode(null, new[] { varY.Write(fCall.ResultLocation!) });
        var fEval = new SingleExitNode(yAssign, fCall.CodeGraph);
        var hEval = new SingleExitNode(fEval, hCall.CodeGraph);
        var gEval = new SingleExitNode(hEval, gCall.CodeGraph);
        var xMainAssign = new SingleExitNode(gEval, new[] { varX.Write(0) });

        // f
        var fRet = new SingleExitNode(null, new[] { varA.Value + varB.Value });

        // g
        var gRet = new SingleExitNode(null, new[] { varX.Value });
        var xGInc = new SingleExitNode(gRet, new[] { varX.Write(varX.Value + 1) });

        // h
        var hRet = new SingleExitNode(null, new[] { varX.Value });
        var xHInc = new SingleExitNode(hRet, new[] { varX.Write(varX.Value + 2) });

        // wrap in prologue and epilogue
        var mainRoot = WrapInContext(mainContext, xMainAssign, new[] { yAssign }, null);
        var fRoot = WrapInContext(fContext, fRet, new[] { fRet }, fCall.ResultLocation);
        var gRoot = WrapInContext(gContext, xGInc, new[] { gRet }, gCall.ResultLocation);
        var hRoot = WrapInContext(hContext, xHInc, new[] { hRet }, hCall.ResultLocation);

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
        var functionContextMap = FunctionContextMapProcessor.Process(ast, nameResolution, new FunctionFactory());
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);
        var functionCodeTreeMap = FunctionCodeTreeMapGenerator.Process(ast,
            root => ControlFlowAnalyzer.UnravelControlFlow(root, nameResolution, functionContextMap, SideEffectsAnalyzer.PullOutSideEffects));

        foreach (var (fun, codeTree) in expected)
        {
            Assert.True(functionCodeTreeMap.ContainsKey(fun));
            Assert.Equal(codeTree, functionCodeTreeMap[fun], new CfgIsomorphismComparer());
        }
    }

    private static CodeTreeRoot WrapInContext(IFunctionContext context, CodeTreeRoot graphRoot, IReadOnlyList<CodeTreeRoot> epiloguePredecessors, CodeTreeValueNode? valToReturn)
    {
        var prologue = context.GeneratePrologue();
        var epilogue = context.GenerateEpilogue(valToReturn);

        prologue[^1].NextTree ??= graphRoot;

        foreach (var node in epiloguePredecessors)
        {
            switch (node)
            {
                case SingleExitNode singleExitNode:
                    singleExitNode.NextTree ??= epilogue[0];
                    break;
                case ConditionalJumpNode conditionalJumpNode:
                    conditionalJumpNode.TrueCase ??= epilogue[0];
                    conditionalJumpNode.FalseCase ??= epilogue[0];
                    break;
            }
        }

        return prologue[0];
    }
}
