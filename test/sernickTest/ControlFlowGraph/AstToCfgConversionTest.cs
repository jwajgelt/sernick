namespace sernickTest.ControlFlowGraph;

using sernick.Ast;
using static Ast.Helpers.AstNodesExtensions;

public class AstToCfgConversionTest
{
    [Fact]
    public void SimpleAddition()
    {
        // var a = 1;
        // var b = 2;
        // var c = a + b;

        _ = Program
        (
            Var("a", 1),
            Var("b", 2),
            Var<IntType>("c", "a".Plus("b"))
        );
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
            If("a".Eq("b")).Then("a".Assign(3)).Else("b".Assign(4)).Get(out _)
        );
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
            Fun<IntType>("f").Parameter<IntType>("n").Body
            (
                If("n".Leq(1)).Then(Return(1)),
                Return("f".Call().Argument("n".Minus(1)).Get(out _)
                    .Plus("f".Call().Argument("n".Minus(2)).Get(out _)))
            ).Get(out _),
            "f".Call().Argument(Literal(5)).Get(out _)
        );
    }

    [Fact]
    public void SimpleLoop()
    {
        // var x : Int = 0;
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
                If("x".Eq(10)).Then(Break).Get(out _)
            )
        );
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
            Fun<IntType>("f").Parameter<IntType>("x").Body
            (
                Fun<IntType>("g").Parameter<IntType>("y").Body
                (
                    Return("y".Plus("y"))
                ).Get(out _),
                Return("g".Call().Argument(Value("x")).Get(out _).Plus("g".Call().Argument("x".Plus(1)).Get(out _)))
            ).Get(out _)
        );
    }

    [Fact]
    public void DeepFunctionCall()
    {
        // fun f1(p1 : Int) : Int {
        //     var v1 = p1;
        //
        //     fun f2(p2 : Int) : Int {
        //         var v2 = v1 + p2;
        //         v1 = v1 + v2;
        //
        //         fun f3(p3 : Int) : Int {
        //             var v3 = v1 + v2 + p3;
        //             v2 = v2 + v3;
        //  
        //             fun f4(p4 : Int) : Int {
        //                 var v4 = v1 + v2 + v3 + p4;
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

                            Return("f2".Call().Argument(Value("v3")).Get(out _))
                        ).Get(out _),

                        Return("f4".Call().Argument(Value("v3")).Get(out _))
                    ).Get(out _),

                    Return("f3".Call().Argument(Value("v2")).Get(out _))
                ).Get(out _),

                Return("f2".Call().Argument(Value("v1")).Get(out _))
            ).Get(out _),
            "f1".Call().Argument(Literal(1)).Get(out _)
        );
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
            Fun<IntType>("f").Parameter<IntType>("x", Literal(1)).Parameter<IntType>("y", Literal(2)).Body
            (
                Fun<IntType>("g").Parameter<IntType>("z", Literal(3)).Body
                (
                    Return(Value("z"))
                ),
                Return("x".Plus("y").Plus("g".Call().Get(out _)))
            ).Get(out _),
            "f".Call().Argument(Literal(3)).Get(out _)
        );
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
            ).Get(out _)
        );
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
                    "f".Call().Get(out _)
                ).Get(out _),

                Fun<UnitType>("h").Body
                (
                    Fun<UnitType>("z").Body
                    (
                        "g".Call().Get(out _)
                    ).Get(out _),

                    "z".Call().Get(out _)
                ).Get(out _),

                "h".Call().Get(out _)
            ).Get(out _),
            "f".Call().Get(out _)
        );
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
            Fun<UnitType>("f").Parameter<BoolType>("v").Body
            (
                Fun<UnitType>("g").Body
                (
                    "x".Assign("x".Plus(1)),
                    "f".Call().Argument(Literal(false)).Get(out _)
                ).Get(out _),

                Fun<UnitType>("h").Body
                (
                    "x".Assign("x".Minus(1)),
                    "f".Call().Argument(Literal(true)).Get(out _)
                ).Get(out _),

                If(Value("v")).Then("g".Call().Get(out _)).Else("h".Call().Get(out _)).Get(out _)

            ).Get(out _),
            "f".Call().Argument(Literal(true)).Get(out _)
        );
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
            Fun<BoolType>("f").Parameter<BoolType>("v").Body
            (
                "v".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ).Get(out _),
            Fun<BoolType>("g").Parameter<BoolType>("v").Body
            (
                "v".Assign("x".Plus(1)),
                Return("v".Leq("x"))
            ).Get(out _),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScOr("g".Call().Argument(Value("y")).Get(out _)))
                    .Then(Break).Get(out _),
                "y".Assign("y".Plus(1))
            )
        );
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
            Fun<BoolType>("f").Parameter<BoolType>("v").Body
            (
                "v".Assign("x".Plus(1)),
                Return("v".Leq(5))
            ).Get(out _),
            Fun<BoolType>("g").Parameter<BoolType>("v").Body
            (
                "v".Assign("x".Plus(1)),
                Return("v".Leq("x"))
            ).Get(out _),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScAnd("g".Call().Argument(Value("y")).Get(out _)))
                    .Then(Break).Get(out _),
                "y".Assign("y".Plus(1))
            )
        );
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
            Fun<BoolType>("f").Parameter<BoolType>("v").Body
            (
                Return("v".Leq(5))
            ).Get(out _),
            Fun<BoolType>("g").Parameter<BoolType>("v").Body
            (
                Return("v".Leq("x"))
            ).Get(out _),
            Fun<BoolType>("h").Parameter<BoolType>("v").Body
            (
                Return("x".Leq("v"))
            ).Get(out _),

            Var("y", 0),
            Loop
            (
                If("f".Call().Argument(Value("y")).Get(out _).ScAnd
                    (
                        "g".Call().Argument(Value("y")).Get(out _).ScOr("h".Call().Argument(Value("y")).Get(out _))
                    ))
                    .Then(Break).Get(out _),
                "y".Assign("y".Plus(1))
            )
        );
    }
}
