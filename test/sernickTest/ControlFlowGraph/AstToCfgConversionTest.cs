namespace sernickTest.ControlFlowGraph;

using sernick.Ast;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Ast.Helpers.AstNodesExtensions;
using sernick.Compiler.Function;
using sernick.Ast.Nodes;
using Moq;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Analysis.NameResolution;
using sernickTest.Diagnostics;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.VariableAccess;

public class AstToCfgConversionTest
{
    private const int PointerSize = 8;
    private CodeTreeValueNode displayAddress = new Constant(new RegisterValue(0)); // TODO use GlobalAddress after it's merged
    // TODO call AST -> CFG conversion and compare result to what's expected

    private CodeTreeRoot empty = new SingleExitNode(null, new CodeTreeNode[] {});

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

        var A = Reg(new Register());
        var B = Reg(new Register());
        var C = Reg(new Register());

        var c = new SingleExitNode(null, new[] { C.Write(A.Value + B.Value) });
        var ab = new SingleExitNode(c, new[] { A.Write(1), B.Write(2) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var A = Reg(new Register());
        var B = Reg(new Register());

        var b4 = new SingleExitNode(null, new[] { B.Write(4) });
        var a3 = new SingleExitNode(null, new[] { A.Write(3) });
        var cond = new BinaryOperationNode(BinaryOperation.Equal, A.Value, B.Value);
        var ifBlock = new ConditionalJumpNode(a3, b4, cond);
        var ab = new SingleExitNode(ifBlock, new[] { A.Write(1), B.Write(2) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var N = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramN }, true);

        var fCall = fContext.GenerateCall(new[] { new Constant(new RegisterValue(5)) });
        var fCallInner1 = fContext.GenerateCall(new[] { N.Value - 1 });
        var fCallInner2 = fContext.GenerateCall(new[] { N.Value - 2 });

        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var retF = new SingleExitNode(null, new[] { fCallInner1.ResultLocation! + fCallInner2.ResultLocation! });
        var fCallsInner = new SingleExitNode(retF, fCallInner1.CodeGraph.Concat(fCallInner2.CodeGraph).ToList());
        var ret1 = new SingleExitNode(null, new[] { new Constant(new RegisterValue(1)) });
        var ifBlock = new ConditionalJumpNode(ret1, fCallsInner, N.Value <= 1);

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var X = Reg(new Register());

        // TODO loop
        // var cond = new BinaryOperationNode(BinaryOperation.Equal, X.Value, 10);
        var loopBlock = empty;
        var x = new SingleExitNode(loopBlock, new[] { X.Write(0) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var X = Reg(HardwareRegister.RDI);
        var Y = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramX }, true);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { paramY }, true);

        var gCall1 = gContext.GenerateCall(new[] { X.Value - 1 });
        var gCall2 = gContext.GenerateCall(new[] { X.Value - 2 });

        var gRet = new SingleExitNode(null, new[] { Y.Value + Y.Value });
        var fRet = new SingleExitNode(null, new[] { gCall1.ResultLocation! + gCall2.ResultLocation! });
        var gCalls = new SingleExitNode(fRet, gCall1.CodeGraph.Concat(gCall2.CodeGraph).ToList());

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
            {main, empty},
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

        var V1 = Mem(Mem(displayAddress + 1*PointerSize).Value);
        var V2 = Mem(Mem(displayAddress + 2*PointerSize).Value);
        var V3 = Mem(Mem(displayAddress + 3*PointerSize).Value);
        var V4 = Mem(Mem(displayAddress + 4*PointerSize).Value);
        var P = Reg(HardwareRegister.RDI); // P1,P2,P3,P4 are always under RDI 

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var f1Context = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramP1 }, true);
        var f2Context = funFactory.CreateFunction(f1Context, new IFunctionParam[] { paramP2 }, true);
        var f3Context = funFactory.CreateFunction(f2Context, new IFunctionParam[] { paramP3 }, true);
        var f4Context = funFactory.CreateFunction(f3Context, new IFunctionParam[] { paramP4 }, true);

        var f1Call = f1Context.GenerateCall(new[] { new Constant(new RegisterValue(1)) });
        var f2Callv1 = f2Context.GenerateCall(new[] { V1.Value });
        var f3Call = f2Context.GenerateCall(new[] { V2.Value });
        var f4Call = f2Context.GenerateCall(new[] { V3.Value });
        var f2Callv3 = f2Context.GenerateCall(new[] { V3.Value });
        
        var f4Ret = new SingleExitNode(null, new[] { f2Callv3.ResultLocation! });
        var f2Callv3Tree = new SingleExitNode(f4Ret, f2Callv3.CodeGraph);
        var v1v4 = new SingleExitNode(f2Callv3Tree, new[] { V1.Write(V4.Value) });
        var v4 = new SingleExitNode(v1v4, new[] { V4.Write(V1.Value + V2.Value + V3.Value + P.Value )});
        
        var f3Ret = new SingleExitNode(null, new[] { f4Call.ResultLocation! });
        var f4CallTree = new SingleExitNode(f3Ret, f4Call.CodeGraph);
        var v2Plusv3 = new SingleExitNode(f4CallTree, new[] { V2.Write(V2.Value + V3.Value) });
        var v3 = new SingleExitNode(v2Plusv3, new[] { V3.Write(V1.Value + V2.Value + P.Value )});
        
        var f2Ret = new SingleExitNode(null, new[] { f3Call.ResultLocation! });
        var f3CallTree = new SingleExitNode(f2Ret, f3Call.CodeGraph);
        var v1Plusv2 = new SingleExitNode(f3CallTree, new[] { V1.Write(V1.Value + V2.Value) });
        var v2 = new SingleExitNode(v1Plusv2, new[] { V2.Write(V1.Value + P.Value)});
        
        var f1Ret = new SingleExitNode(null, new[] { f2Callv1.ResultLocation! });
        var f2Callv1Tree = new SingleExitNode(f1Ret, f2Callv1.CodeGraph);
        var v1 = new SingleExitNode(f2Callv1Tree, new[] { V1.Write(P.Value)});

        var f1CallTree = new SingleExitNode(null, f1Call.CodeGraph);

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var X = Reg(HardwareRegister.RDI);
        var Y = Reg(HardwareRegister.RSI);
        var Z = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramX, paramY }, true);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] { paramZ }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { });
        
        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var fRet = new SingleExitNode(null, new[] { X.Value + Y.Value + gCall.ResultLocation! });
        var gCallTree = new SingleExitNode(fRet, gCall.CodeGraph);
        var gRet = new SingleExitNode(null, new[] { Z.Value });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var Xf = Reg(new Register());
        var Xg = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] {}, true);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] {}, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] {});
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] {});

        var fRet = new SingleExitNode(null, new[] { gCall.ResultLocation! + Xf.Value });
        var gCallTree = new SingleExitNode(fRet, gCall.CodeGraph);
        var x1 = new SingleExitNode(gCallTree, new[] { Xf.Write(1) });
        var gRet = new SingleExitNode(null, new[] { Xg.Value });
        var xxx = new SingleExitNode(gRet, new[] { Xg.Write(Xg.Value + Xg.Value) });
        var x2 = new SingleExitNode(xxx, new[] { Xg.Write(2) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
            {main, empty},
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
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] {}, false);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] {}, false);
        var hContext = funFactory.CreateFunction(fContext, new IFunctionParam[] {}, false);
        var zContext = funFactory.CreateFunction(hContext, new IFunctionParam[] {}, false);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] {});
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] {});
        var hCall = gContext.GenerateCall(new CodeTreeValueNode[] {});
        var zCall = gContext.GenerateCall(new CodeTreeValueNode[] {});

        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var hCallTree = new SingleExitNode(null, hCall.CodeGraph);
        var zCallTree = new SingleExitNode(null, zCall.CodeGraph);

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var X = Mem(Mem(displayAddress).Value);
        var V = Reg(HardwareRegister.RDI);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramV }, false);
        var gContext = funFactory.CreateFunction(fContext, new IFunctionParam[] {}, false);
        var hContext = funFactory.CreateFunction(fContext, new IFunctionParam[] {}, false);

        var fCallInMain = fContext.GenerateCall(new CodeTreeValueNode[] { new Constant(new RegisterValue(1)) });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] {});
        var hCall = hContext.GenerateCall(new CodeTreeValueNode[] {});
        var fCallInG = fContext.GenerateCall(new CodeTreeValueNode[] { new Constant(new RegisterValue(0)) });
        var fCallInH = fContext.GenerateCall(new CodeTreeValueNode[] { new Constant(new RegisterValue(1)) });

        var fCallInMainTree = new SingleExitNode(null, fCallInMain.CodeGraph);
        var x1 = new SingleExitNode(fCallInMainTree, new[] { X.Write(1) });
        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var hCallTree = new SingleExitNode(null, hCall.CodeGraph);
        var ifBlock = new ConditionalJumpNode(gCallTree, hCallTree, V.Value);
        var fCallInHTree = new SingleExitNode(null, fCallInH.CodeGraph);
        var xMinus1 = new SingleExitNode(fCallInHTree, new[] { X.Write(X.Value - 1) });
        var fCallInGTree = new SingleExitNode(null, fCallInG.CodeGraph);
        var xPlus1 = new SingleExitNode(fCallInGTree, new[] { X.Write(X.Value + 1) });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
            {main, fCallInMainTree},
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

        var X = Mem(Mem(displayAddress).Value);
        var V = Reg(HardwareRegister.RDI);
        var Y = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramF }, true);
        var gContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramG }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { Y.Value });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { Y.Value });

        var yPlus1 = new SingleExitNode(null, new[] { Y.Write(Y.Value + 1) });
        var cond2 = new ConditionalJumpNode(null, yPlus1, gCall.ResultLocation!);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(null, gCallTree, fCall.ResultLocation!);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        // TODO not sure how loops are supposed to work
        var xy = new SingleExitNode(cond1, new CodeTreeNode[] { X.Write(1), Y.Write(0) });
        
        var gRet = new SingleExitNode(null, new[] { V.Value <= X.Value });
        var xPlus1InG = new SingleExitNode(gRet, new[] { X.Write(X.Value + 1) });
        var fRet = new SingleExitNode(null, new CodeTreeNode[] { X.Write(X.Value + 1), V.Value <= 5 });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var X = Mem(Mem(displayAddress).Value);
        var V = Reg(HardwareRegister.RDI);
        var Y = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramF }, true);
        var gContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramG }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { Y.Value });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { Y.Value });

        var yPlus1 = new SingleExitNode(null, new[] { Y.Write(Y.Value + 1) });
        var cond2 = new ConditionalJumpNode(null, yPlus1, gCall.ResultLocation!);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(gCallTree, yPlus1, fCall.ResultLocation!);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        // TODO not sure how loops are supposed to work
        var xy = new SingleExitNode(cond1, new CodeTreeNode[] { X.Write(1), Y.Write(0) });
        
        var gRet = new SingleExitNode(null, new[] { V.Value <= X.Value });
        var xPlus1InG = new SingleExitNode(gRet, new[] { X.Write(X.Value + 1) });
        var fRet = new SingleExitNode(null, new CodeTreeNode[] { X.Write(X.Value + 1), V.Value <= 5 });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
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

        var X = Mem(Mem(displayAddress).Value);
        var V = Reg(HardwareRegister.RDI);
        var Y = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramF }, true);
        var gContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramG }, true);
        var hContext = funFactory.CreateFunction(mainContext, new IFunctionParam[] { paramH }, true);

        var fCall = fContext.GenerateCall(new CodeTreeValueNode[] { Y.Value });
        var gCall = gContext.GenerateCall(new CodeTreeValueNode[] { Y.Value });
        var hCall = hContext.GenerateCall(new CodeTreeValueNode[] { Y.Value });

        var yPlus1 = new SingleExitNode(null, new[] { Y.Write(Y.Value + 1) });
        var cond3 = new ConditionalJumpNode(null, yPlus1, hCall.ResultLocation!);
        var hCallTree = new SingleExitNode(cond3, gCall.CodeGraph);
        var cond2 = new ConditionalJumpNode(null, hCallTree, gCall.ResultLocation!);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(gCallTree, yPlus1, fCall.ResultLocation!);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        // TODO not sure how loops are supposed to work
        var xy = new SingleExitNode(cond1, new CodeTreeNode[] { X.Write(10), Y.Write(0) });
        
        var hRet = new SingleExitNode(null, new[] { X.Value <= V.Value });
        var gRet = new SingleExitNode(null, new[] { V.Value <= X.Value });
        var fRet = new SingleExitNode(null, new[] { V.Value <= 5 });

        Verify(main, new Dictionary<FunctionDefinition, CodeTreeRoot>{
            {main, xy},
            {f, fRet},
            {g, gRet},
            {h, hRet}
        });
    }

    private void Verify(FunctionDefinition ast, IReadOnlyDictionary<FunctionDefinition, CodeTreeRoot> expected)
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
}
