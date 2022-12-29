namespace sernickTest.Ast.Analysis;

using Diagnostics;
using sernick.Ast;
using sernick.Ast.Analysis.VariableAccess;
using static Helpers.AstNodesExtensions;
using static Helpers.NameResolutionResultBuilder;

public class VariableAccessMapBuilderTest
{
    [Fact]
    public void Builder_adds_VariableRead()
    {
        // var x;
        // fun foo() { bar(x); }
        var ast = Program(
            Var("x", out var xDeclare),
            Fun<UnitType>("foo")
                .Body(
                    "bar".Call().Argument(Value("x", out var xVal)),
                    Close
                    )
                .Get(out var foo)
        );
        var nameResolution = NameResolution().WithVars((xVal, xDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.Single(variableAccessMap[foo], item => item.Equals((xDeclare, VariableAccessMode.ReadOnly)));
    }

    [Fact]
    public void Builder_adds_VariableWrite()
    {
        // var x;
        // fun foo() { x = 1 }
        var ast = Program(
            Var("x", out var xDeclare),
            Fun<UnitType>("foo")
                .Body(
                    "x".Assign(1, out var xAssign),
                    Close)
                .Get(out var foo)
            );
        var nameResolution = NameResolution().WithAssigns((xAssign, xDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.Single(variableAccessMap[foo], item => item.Equals((xDeclare, VariableAccessMode.WriteAndRead)));
    }

    [Fact]
    public void Builder_adds_VariableWrite_When_AssignInDeclaration()
    {
        // fun foo() { var x = 1 }
        var ast = Program(
            Fun<UnitType>("foo")
                .Body(Var("x", 1, out var xDeclare))
                .Get(out var foo)
        );
        var nameResolution = NameResolution();
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.Single(variableAccessMap[foo], item => item.Equals((xDeclare, VariableAccessMode.WriteAndRead)));
    }

    [Fact]
    public void Builder_overwrites_Read_with_WriteAdnRead()
    {
        // var x;
        // fun foo() {
        //     bar(x);
        //     x = 1;
        // }
        var ast = Program(
            Var("x", out var xDeclare),
            Fun<UnitType>("foo")
                .Body(
                    "bar".Call().Argument(Value("x", out var xVal)),
                    "x".Assign(1, out var xAssign),
                    Close
                    )
                .Get(out var foo)
        );
        var nameResolution = NameResolution()
            .WithVars((xVal, xDeclare))
            .WithAssigns((xAssign, xDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.Single(variableAccessMap[foo], item => item.Equals((xDeclare, VariableAccessMode.WriteAndRead)));
    }

    [Fact]
    public void Builder_handles_NestedFunctions()
    {
        // var x;
        // fun f1() {
        //     x = 3;
        //     var y;
        //     fun f2() {
        //         y = 2;
        //         var z;
        //         fun f3() {
        //             z = 1;
        //         }
        //     }
        // }
        var ast = Program(
            Var("x", out var xDeclare),
            Fun<UnitType>("f1")
                .Body(
                    "x".Assign(3, out var xAssign),
                    Var("y", out var yDeclare),
                    Fun<UnitType>("f2")
                        .Body(
                            "y".Assign(2, out var yAssign),
                            Var("z", out var zDeclare),
                            Fun<UnitType>("f3")
                                .Body(
                                    "z".Assign(1, out var zAssign),
                                    Close
                                    )
                                .Get(out var f3Def)
                            )
                        .Get(out var f2Def)
                    )
                .Get(out var f1Def)
            );
        var nameResolution = NameResolution().WithAssigns(
            (xAssign, xDeclare),
            (yAssign, yDeclare),
            (zAssign, zDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.Single(variableAccessMap[f1Def], item => item.Equals((xDeclare, VariableAccessMode.WriteAndRead)));
        Assert.Single(variableAccessMap[f2Def], item => item.Equals((yDeclare, VariableAccessMode.WriteAndRead)));
        Assert.Single(variableAccessMap[f3Def], item => item.Equals((zDeclare, VariableAccessMode.WriteAndRead)));
    }

    [Fact]
    public void Builder_handles_complex_Assignments()
    {
        // var x;
        // var y;
        // fun foo () {
        //     y = { x = 1; x }
        // }
        var ast = Program(
            Var("x", out var xDeclare),
            Var("y", out var yDeclare),
            Fun<UnitType>("foo")
                .Body(
                    "y".Assign(
                        Block(
                            "x".Assign(1, out var xAssign),
                            Value("x", out var xVal)),
                        out var yAssign
                        )
                )
                .Get(out var foo)
            );
        var nameResolution = NameResolution()
            .WithAssigns((xAssign, xDeclare), (yAssign, yDeclare))
            .WithVars((xVal, xDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.True(variableAccessMap.HasExclusiveWriteAccess(foo, xDeclare));
        Assert.True(variableAccessMap.HasExclusiveWriteAccess(foo, yDeclare));

        Assert.Equal(2, variableAccessMap[foo].Count());
        Assert.Contains((xDeclare, VariableAccessMode.WriteAndRead), variableAccessMap[foo]);
        Assert.Contains((yDeclare, VariableAccessMode.WriteAndRead), variableAccessMap[foo]);
    }

    [Fact]
    public void BuiltMap_recognises_ExclusiveWriteAccess_Case1()
    {
        // var x;
        // fun foo() {
        //     x = 1;
        //     x = 2;
        // }
        // fun bar() { }
        var ast = Program(
            Var("x", out var xDeclare),
            Fun<UnitType>("foo")
                .Body(
                    "x".Assign(1, out var xAssign1),
                    "x".Assign(1, out var xAssign2),
                    Close)
                .Get(out var foo),
            Fun<UnitType>("bar")
                .Body(Close)
                .Get(out var bar)
        );
        var nameResolution = NameResolution().WithAssigns(
            (xAssign1, xDeclare),
            (xAssign2, xDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.True(variableAccessMap.HasExclusiveWriteAccess(foo, xDeclare));
        Assert.False(variableAccessMap.HasExclusiveWriteAccess(bar, xDeclare));
    }

    [Fact]
    public void BuiltMap_recognises_ExclusiveWriteAccess_Case2()
    {
        // var x;
        // fun foo() { x = 1 }
        // fun bar() { x = 2 }
        var ast = Program(
            Var("x", out var xDeclare),
            Fun<UnitType>("foo")
                .Body("x".Assign(1, out var xAssign1))
                .Get(out var foo),
            Fun<UnitType>("bar")
                .Body("x".Assign(2, out var xAssign2))
                .Get(out var bar)
        );
        var nameResolution = NameResolution().WithAssigns(
            (xAssign1, xDeclare),
            (xAssign2, xDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.False(variableAccessMap.HasExclusiveWriteAccess(foo, xDeclare));
        Assert.False(variableAccessMap.HasExclusiveWriteAccess(bar, xDeclare));
    }

    [Fact]
    public void InnerFunctionConstAssignment_Case1()
    {
        // const x;
        // fun foo() { x = 1; }
        var ast = Program(
            Const("x", out var xDeclare),
            Fun<UnitType>("foo")
                .Body("x".Assign(1, out var xAssign1))
                .Get(out var foo)
        );
        var nameResolution = NameResolution().WithAssigns((xAssign1, xDeclare));
        var diagnostics = new FakeDiagnostics();
        VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        var expected = new InnerFunctionConstVariableWriteError(ast, xDeclare, foo, xAssign1);
        Assert.Equal(expected, diagnostics.DiagnosticItems.Single());
    }

    [Fact]
    public void InnerFunctionConstAssignment_Case2()
    {
        // fun foo() {
        //     const x;
        //     fun bar() {
        //         x = 1;
        //     }
        // }
        var ast = Program(
            Fun<UnitType>("foo")
                .Body(
                    Const("x", out var xDeclare),
                    Fun<UnitType>("bar")
                        .Body("x".Assign(1, out var xAssign1))
                        .Get(out var bar))
                .Get(out var foo)
        );
        var nameResolution = NameResolution().WithAssigns((xAssign1, xDeclare));
        var diagnostics = new FakeDiagnostics();
        VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        var expected = new InnerFunctionConstVariableWriteError(foo, xDeclare, bar, xAssign1);
        Assert.Equal(expected, diagnostics.DiagnosticItems.Single());
    }

    [Fact]
    public void NestedInnerFunctionConstAssignment()
    {
        // const x;
        // fun a() {
        //     fun b() {
        //         fun c() {
        //             x = 1;
        //         }
        //     }
        // }
        var ast = Program(
            Const("x", out var xDeclare),
            Fun<UnitType>("a")
                .Body(
                    Fun<UnitType>("b")
                        .Body(
                            Fun<UnitType>("c")
                                .Body("x".Assign(1, out var xAssign1))
                                .Get(out var c)
                        )
                )
        );
        var nameResolution = NameResolution().WithAssigns((xAssign1, xDeclare));
        var diagnostics = new FakeDiagnostics();
        VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        var expected = new InnerFunctionConstVariableWriteError(ast, xDeclare, c, xAssign1);
        Assert.Equal(expected, diagnostics.DiagnosticItems.Single());
    }

    [Fact]
    public void MultipleInnerFunctionConstAssignments()
    {
        // const x;
        // fun a() {
        //     const y;
        //     fun b() {
        //         y = 1;
        //         fun c() {
        //             x = 1;
        //         }
        //     }
        // }
        var ast = Program(
            Const("x", out var xDeclare),
            Fun<UnitType>("a")
                .Body(
                    Const("y", out var yDeclare),
                    Fun<UnitType>("b")
                        .Body(
                            "y".Assign(1, out var yAssign1),
                            Fun<UnitType>("c")
                                .Body("x".Assign(1, out var xAssign1))
                                .Get(out var c)
                        )
                        .Get(out var b)
                )
                .Get(out var a)
        );
        var nameResolution = NameResolution().WithAssigns((xAssign1, xDeclare), (yAssign1, yDeclare));
        var diagnostics = new FakeDiagnostics();
        VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        var xError = new InnerFunctionConstVariableWriteError(ast, xDeclare, c, xAssign1);
        var yError = new InnerFunctionConstVariableWriteError(a, yDeclare, b, yAssign1);

        Assert.Equal(2, diagnostics.DiagnosticItems.LongCount());
        Assert.Contains(xError, diagnostics.DiagnosticItems);
        Assert.Contains(yError, diagnostics.DiagnosticItems);
    }
}
