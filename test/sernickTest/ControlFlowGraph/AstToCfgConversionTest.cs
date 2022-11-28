namespace sernickTest.ControlFlowGraph;

using sernick.Ast;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Ast.Helpers.AstNodesExtensions;
using sernick.Compiler.Function;

public class AstToCfgConversionTest
{
    private const int PointerSize = 8;
    private CodeTreeValueNode displayAddress = new Constant(new RegisterValue(0)); // TODO idk where it is
    // TODO call AST -> CFG conversion and compare result to what's expected

    [Fact]
    public void SimpleAddition()
    {
        // var a = 1;
        // var b = 2;
        // var c : Int = a + b;

        _ = Program
        (
            Var("a", 1),
            Var("b", 2),
            Var<IntType>("c", "a".Plus("b"))
        );

        var A = Reg(new Register());
        var B = Reg(new Register());
        var C = Reg(new Register());

        var c = new SingleExitNode(null, C.Write(A.Value + B.Value));
        var b = new SingleExitNode(c, B.Write(2));
        var a = new SingleExitNode(b, A.Write(1));

        var expected = new List<CodeTreeRoot> {a,b,c};
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

        _ = Program
        (
            Var("a", 1),
            Var("b", 2),
            If("a".Eq("b")).Then("a".Assign(3)).Else("b".Assign(4))
        );

        var A = Reg(new Register());
        var B = Reg(new Register());

        var b4 = new SingleExitNode(null, B.Write(4));
        var a3 = new SingleExitNode(null, A.Write(3));
        var cond = new SingleExitNode(null, new BinaryOperationNode(
            BinaryOperation.Equal, A.Value, B.Value
        ));
        var ifBlock = new ConditionalJumpNode(a3, b4, cond);
        var b2 = new SingleExitNode(ifBlock, B.Write(2));
        var a1 = new SingleExitNode(b2, A.Write(1));

        var expected = new List<CodeTreeRoot> {a1,b2,ifBlock,cond,a3,b4};
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

        _ = Program
        (
            Fun<IntType>("f").Parameter<IntType>("n", out var paramN).Body
            (
                If("n".Leq(1)).Then(Return(1)),
                Return("f".Call().Argument("n".Minus(1)).Get(out _)
                    .Plus("f".Call().Argument("n".Minus(2))))
            ),
            "f".Call().Argument(Literal(5))
        );

        var N = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramN }, false);

        var fCall = fContext.GenerateCall(new[] { new Constant(new RegisterValue(5)) });
        var fCallInner1 = fContext.GenerateCall(new[] { N.Value - 1 });
        var fCallInner2 = fContext.GenerateCall(new[] { N.Value - 2 });

        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var fCallInner1Tree = new SingleExitNode(null, fCallInner1.CodeGraph);
        var fCallInner2Tree = new SingleExitNode(null, fCallInner2.CodeGraph);
        var retF = new SingleExitNode(null, (RegisterRead)fCallInner1.ResultLocation + (RegisterRead)fCallInner2.ResultLocation);
        var ret1 = new SingleExitNode(null, 1);
        var ifBlock = new ConditionalJumpNode(ret1, retF, N.Value <= 1);

        var expected = new List<CodeTreeRoot> {fCallTree,ifBlock,ret1,retF,fCallInner1Tree,fCallInner2Tree};
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
        _ = Program
        (
            Var("x", 0),
            Loop
            (
                "x".Assign("x".Plus(1)),
                If("x".Eq(10)).Then(Break)
            )
        );

        var X = Reg(new Register());

        var cond = new SingleExitNode(null, new BinaryOperationNode(
            BinaryOperation.Equal, X.Value, 10
        ));

        // TODO loop

        var expected = new List<CodeTreeRoot> {};
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

        _ = Program
        (
            Fun<IntType>("f").Parameter<IntType>("x", out var paramX).Body
            (
                Fun<IntType>("g").Parameter<IntType>("y", out var paramY).Body
                (
                    Return("y".Plus("y"))
                ),
                Return("g".Call().Argument(Value("x")).Get(out _).Plus("g".Call().Argument("x".Plus(1))))
            )
        );

        var X = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Y = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize); // TODO ? rbp changes so this should be correct

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramX }, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramY }, false);

        var gCall1 = gContext.GenerateCall(new[] { X.Value - 1 });
        var gCall2 = gContext.GenerateCall(new[] { X.Value - 2 });

        var gRet = new SingleExitNode(null, Y.Value + Y.Value);
        var gCall1Tree = new SingleExitNode(null, gCall1.CodeGraph);
        var gCall2Tree = new SingleExitNode(null, gCall2.CodeGraph);
        var fRet = new SingleExitNode(null, (RegisterRead)gCall1.ResultLocation + (RegisterRead)gCall2.ResultLocation);

        var expected = new List<CodeTreeRoot> {fRet,gCall1Tree,gCall2Tree,gRet};
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

        _ = Program
        (
            Fun<IntType>("f1").Parameter<IntType>("p1").Body
            (
                Var<IntType>("v1", Value("p1")),

                Fun<IntType>("f2").Parameter<IntType>("p2").Body
                (
                    Var<IntType>("v2", "v1".Plus("p2")),
                    "v1".Assign("v1".Plus("v2")),

                    Fun<IntType>("f3").Parameter<IntType>("p3").Body
                    (
                        Var<IntType>("v3", "v1".Plus("v2").Plus("p3")),
                        "v2".Assign("v2".Plus("v3")),

                        Fun<IntType>("f4").Parameter<IntType>("p4").Body
                        (
                            Var<IntType>("v4", "v1".Plus("v2").Plus("v3").Plus("p4")),
                            "v1".Assign(Value("v4")),

                            Return("f2".Call().Argument(Value("v3")))
                        ),

                        Return("f4".Call().Argument(Value("v3")))
                    ),

                    Return("f3".Call().Argument(Value("v2")))
                ),

                Return("f2".Call().Argument(Value("v1")))
            ),
            "f1".Call().Argument(Literal(1))
        );

        // TODO O_O
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

        _ = Program
        (
            Fun<IntType>("f")
            .Parameter<IntType>("x", Literal(1), out var paramX)
            .Parameter<IntType>("y", Literal(2), out var paramY)
            .Body
            (
                Fun<IntType>("g").Parameter("z", Literal(3), out var paramZ).Body
                (
                    Return(Value("z"))
                ),
                Return("x".Plus("y").Plus("g".Call()))
            ),
            "f".Call().Argument(Literal(3))
        );

        var X = Mem(Reg(HardwareRegister.RBP).Value + 3*PointerSize);
        var Y = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Z = Mem(Reg(HardwareRegister.RBP).Value + 3*PointerSize);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramX, paramY }, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramZ }, false);

        var fCall = fContext.GenerateCall(new[] { new Constant(new RegisterValue(3)), new Constant(new RegisterValue(2)) });
        var gCall = gContext.GenerateCall(new[] { new Constant(new RegisterValue(3)) });
        
        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var fRet = new SingleExitNode(null, X.Value + Y.Value + (RegisterRead)gCall.ResultLocation); // TODO fix: call is not a value
        var gRet = new SingleExitNode(null, Z.Value);

        var expected = new List<CodeTreeRoot> {fRet,gRet,fCallTree,gCallTree};
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

        _ = Program
        (
            Fun<IntType>("f").Body
            (
                Var("x", 1),
                Fun<IntType>("g").Body
                (
                    Var("x", 2),
                    "x".Assign("x".Plus("x")),
                    Return(Value("x"))
                ),
                Return("g".Call().Get(out _).Plus("x"))
            )
        );

        var Xf = Reg(new Register());
        var Xg = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);

        var fCall = fContext.GenerateCall(new CodeTreeNode[] {});
        var gCall = gContext.GenerateCall(new CodeTreeNode[] {});

        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var fRet = new SingleExitNode(null, (RegisterRead)gCall.ResultLocation + Xf.Value); // TODO fix: call is not a value
        var x1 = new SingleExitNode(fRet, Xf.Write(1));
        var gRet = new SingleExitNode(null, Xg.Value);
        var xxx = new SingleExitNode(gRet, Xg.Write(Xg.Value + Xg.Value));
        var x2 = new SingleExitNode(xxx, Xg.Write(2));

        var expected = new List<CodeTreeRoot> {x1,gCallTree,fRet,x2,xxx,gRet};
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

        _ = Program
        (
            Fun<UnitType>("f").Body
            (
                Fun<UnitType>("g").Body
                (
                    "f".Call()
                ),

                Fun<UnitType>("h").Body
                (
                    Fun<UnitType>("z").Body
                    (
                        "g".Call()
                    ),

                    "z".Call()
                ),

                "h".Call()
            ),
            "f".Call()
        );

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var hContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var zContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);

        var fCall = fContext.GenerateCall(new CodeTreeNode[] {});
        var gCall = gContext.GenerateCall(new CodeTreeNode[] {});
        var hCall = gContext.GenerateCall(new CodeTreeNode[] {});
        var zCall = gContext.GenerateCall(new CodeTreeNode[] {});

        var fCallTree = new SingleExitNode(null, fCall.CodeGraph);
        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var hCallTree = new SingleExitNode(null, hCall.CodeGraph);
        var zCallTree = new SingleExitNode(null, zCall.CodeGraph);

        var expected = new List<CodeTreeRoot> {fCallTree,gCallTree,hCallTree,zCallTree};
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

        _ = Program
        (
            Var("x", 1),
            Fun<UnitType>("f").Parameter<BoolType>("v", out var paramV).Body
            (
                Fun<UnitType>("g").Body
                (
                    "x".Assign("x".Plus(1)),
                    "f".Call().Argument(Literal(false))
                ),

                Fun<UnitType>("h").Body
                (
                    "x".Assign("x".Minus(1)),
                    "f".Call().Argument(Literal(true))
                ),

                If(Value("v")).Then("g".Call()).Else("h".Call())

            ),
            "f".Call().Argument(Literal(true))
        );

        var X = Mem(displayAddress);
        var V = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramV }, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var hContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);

        var fCallInMain = fContext.GenerateCall(new CodeTreeNode[] { new Constant(new RegisterValue(1)) });
        var gCall = gContext.GenerateCall(new CodeTreeNode[] {});
        var hCall = hContext.GenerateCall(new CodeTreeNode[] {});
        var fCallInG = fContext.GenerateCall(new CodeTreeNode[] { new Constant(new RegisterValue(0)) });
        var fCallInH = fContext.GenerateCall(new CodeTreeNode[] { new Constant(new RegisterValue(1)) });

        var fCallInMainTree = new SingleExitNode(null, fCallInMain.CodeGraph);
        var x1 = new SingleExitNode(fCallInMainTree, X.Write(1));
        var gCallTree = new SingleExitNode(null, gCall.CodeGraph);
        var hCallTree = new SingleExitNode(null, hCall.CodeGraph);
        var ifBlock = new ConditionalJumpNode(gCallTree, hCallTree, V.Value);
        var fCallInHTree = new SingleExitNode(null, fCallInH.CodeGraph);
        var xMinus1 = new SingleExitNode(fCallInHTree, X.Write(X.Value - 1));
        var fCallInGTree = new SingleExitNode(null, fCallInG.CodeGraph);
        var xPlus1 = new SingleExitNode(fCallInGTree, X.Write(X.Value + 1));

        var expected = new List<CodeTreeRoot> {x1,fCallInMainTree,ifBlock,gCallTree,xPlus1,fCallInGTree,hCallTree,xMinus1,fCallInHTree};
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

        _ = Program
        (
            Var("x", 1),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramF).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramG).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq("x"))
            ),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScOr("g".Call().Argument(Value("y"))))
                    .Then(Break),
                "y".Assign("y".Plus(1))
            )
        );

        var X = Mem(displayAddress);
        var Vf = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Vg = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Y = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramF }, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramG }, false);

        var fCall = fContext.GenerateCall(new CodeTreeNode[] { Y.Value });
        var gCall = gContext.GenerateCall(new CodeTreeNode[] { Y.Value });

        var yPlus1 = new SingleExitNode(null, Y.Write(Y.Value + 1)); // TODO not null
        var cond2 = new ConditionalJumpNode(null, yPlus1, (RegisterRead)gCall.ResultLocation);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(null, gCallTree, (RegisterRead)fCall.ResultLocation);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        // TODO not sure how loops are supposed to work
        var y0 = new SingleExitNode(cond1, Y.Write(0));
        var x1 = new SingleExitNode(y0, X.Write(1));
        
        var gRet = new SingleExitNode(null, Vg.Value <= X.Value);
        var xPlus1InG = new SingleExitNode(gRet, X.Write(X.Value + 1));
        var fRet = new SingleExitNode(null, Vf.Value <= 5);
        var xPlus1InF = new SingleExitNode(fRet, X.Write(X.Value + 1));

        var expected = new List<CodeTreeRoot> {x1,y0,fCallTree,cond1,gCallTree,cond2,yPlus1,xPlus1InF,fRet,xPlus1InG,gRet};
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

        _ = Program
        (
            Var("x", 1),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramF).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramG).Body
            (
                "x".Assign("x".Plus(1)),
                Return("v".Leq("x"))
            ),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScAnd("g".Call().Argument(Value("y"))))
                    .Then(Break),
                "y".Assign("y".Plus(1))
            )
        );

        var X = Mem(displayAddress);
        var Vf = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Vg = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Y = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramF }, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramG }, false);

        var fCall = fContext.GenerateCall(new CodeTreeNode[] { Y.Value });
        var gCall = gContext.GenerateCall(new CodeTreeNode[] { Y.Value });

        var yPlus1 = new SingleExitNode(null, Y.Write(Y.Value + 1)); // TODO not null
        var cond2 = new ConditionalJumpNode(null, yPlus1, (RegisterRead)gCall.ResultLocation);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(gCallTree, yPlus1, (RegisterRead)fCall.ResultLocation);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        // TODO not sure how loops are supposed to work
        var y0 = new SingleExitNode(cond1, Y.Write(0));
        var x1 = new SingleExitNode(y0, X.Write(1));
        
        var gRet = new SingleExitNode(null, Vg.Value <= X.Value);
        var xPlus1InG = new SingleExitNode(gRet, X.Write(X.Value + 1));
        var fRet = new SingleExitNode(null, Vf.Value <= 5);
        var xPlus1InF = new SingleExitNode(fRet, X.Write(X.Value + 1));

        var expected = new List<CodeTreeRoot> {x1,y0,fCallTree,cond1,gCallTree,cond2,yPlus1,xPlus1InF,fRet,xPlus1InG,gRet};
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

        _ = Program
        (
            Var("x", 10),
            Fun<BoolType>("f").Parameter<IntType>("v", out var paramF).Body
            (
                Return("v".Leq(5))
            ),
            Fun<BoolType>("g").Parameter<IntType>("v", out var paramG).Body
            (
                Return("v".Leq("x"))
            ),
            Fun<BoolType>("h").Parameter<IntType>("v", out var paramH).Body
            (
                Return("x".Leq("v"))
            ),

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

        var X = Mem(displayAddress);
        var Vf = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Vg = Mem(Reg(HardwareRegister.RBP).Value + 2*PointerSize);
        var Y = Reg(new Register());

        var funFactory = new FunctionFactory();
        var mainContext = funFactory.CreateFunction(null, new IFunctionParam[] {}, false);
        var fContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramF }, false);
        var gContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramG }, false);
        var hContext = funFactory.CreateFunction(null, new IFunctionParam[] { paramH }, false);

        var fCall = fContext.GenerateCall(new CodeTreeNode[] { Y.Value });
        var gCall = gContext.GenerateCall(new CodeTreeNode[] { Y.Value });
        var hCall = hContext.GenerateCall(new CodeTreeNode[] { Y.Value });

        var yPlus1 = new SingleExitNode(null, Y.Write(Y.Value + 1)); // TODO not null
        var cond3 = new ConditionalJumpNode(null, yPlus1, (RegisterRead)gCall.ResultLocation);
        var hCallTree = new SingleExitNode(cond3, gCall.CodeGraph);
        var cond2 = new ConditionalJumpNode(null, hCallTree, (RegisterRead)gCall.ResultLocation);
        var gCallTree = new SingleExitNode(cond2, gCall.CodeGraph);
        var cond1 = new ConditionalJumpNode(gCallTree, yPlus1, (RegisterRead)fCall.ResultLocation);
        var fCallTree = new SingleExitNode(cond1, fCall.CodeGraph);
        // TODO not sure how loops are supposed to work
        var y0 = new SingleExitNode(cond1, Y.Write(0));
        var x1 = new SingleExitNode(y0, X.Write(1));
        
        var gRet = new SingleExitNode(null, Vg.Value <= X.Value);
        var xPlus1InG = new SingleExitNode(gRet, X.Write(X.Value + 1));
        var fRet = new SingleExitNode(null, Vf.Value <= 5);
        var xPlus1InF = new SingleExitNode(fRet, X.Write(X.Value + 1));

        var expected = new List<CodeTreeRoot> {x1,y0,fCallTree,cond1,gCallTree,cond2,hCallTree,cond3,yPlus1,xPlus1InF,fRet,xPlus1InG,gRet};
    }
}
