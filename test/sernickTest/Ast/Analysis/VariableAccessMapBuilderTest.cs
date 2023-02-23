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
                    Value("x", out var xValue).Assign(Literal(1)),
                    Close)
                .Get(out var foo)
        );
        var nameResolution = NameResolution().WithVars((xValue, xDeclare));
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
                    "bar".Call().Argument(Value("x", out var xValue1)),
                    Value("x", out var xValue2).Assign(Literal(1)),
                    Close
                )
                .Get(out var foo)
        );
        var nameResolution = NameResolution()
            .WithVars((xValue1, xDeclare), (xValue2, xDeclare));
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
                    Value("x", out var xValue).Assign(Literal(3)),
                    Var("y", out var yDeclare),
                    Fun<UnitType>("f2")
                        .Body(
                            Value("y", out var yValue).Assign(Literal(2)),
                            Var("z", out var zDeclare),
                            Fun<UnitType>("f3")
                                .Body(
                                    Value("z", out var zValue).Assign(Literal(1)),
                                    Close
                                )
                                .Get(out var f3Def)
                        )
                        .Get(out var f2Def)
                )
                .Get(out var f1Def)
        );
        var nameResolution = NameResolution().WithVars(
            (xValue, xDeclare),
            (yValue, yDeclare),
            (zValue, zDeclare));
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
                    Value("y", out var yValue).Assign(
                        Block(
                            Value("x", out var xValue1).Assign(Literal(1)),
                            Value("x", out var xValue2)),
                        out _
                    )
                )
                .Get(out var foo)
        );
        var nameResolution = NameResolution()
            .WithVars((xValue1, xDeclare),
                (xValue2, xDeclare),
                (yValue, yDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.True(variableAccessMap.HasExclusiveWriteAccess(foo, xDeclare));
        Assert.True(variableAccessMap.HasExclusiveWriteAccess(foo, yDeclare));

        Assert.Equal(3, variableAccessMap[foo].Count());
        Assert.Contains((xDeclare, VariableAccessMode.WriteAndRead), variableAccessMap[foo]);
        Assert.Contains((yDeclare, VariableAccessMode.WriteAndRead), variableAccessMap[foo]);
        Assert.Contains((VariableAccessMap.HeapMemoryVariable, VariableAccessMode.WriteAndRead),
            variableAccessMap[foo]);
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
                    Value("x", out var xValue1).Assign(Literal(1)),
                    Value("x", out var xValue2).Assign(Literal(1)),
                    Close)
                .Get(out var foo),
            Fun<UnitType>("bar")
                .Body(Close)
                .Get(out var bar)
        );
        var nameResolution = NameResolution().WithVars(
            (xValue1, xDeclare),
            (xValue2, xDeclare));
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
                .Body(Value("x", out var xValue1).Assign(Literal(1)))
                .Get(out var foo),
            Fun<UnitType>("bar")
                .Body(Value("x", out var xValue2).Assign(Literal(2)))
                .Get(out var bar)
        );
        var nameResolution = NameResolution().WithVars(
            (xValue1, xDeclare),
            (xValue2, xDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        Assert.Empty(diagnostics.DiagnosticItems);

        Assert.False(variableAccessMap.HasExclusiveWriteAccess(foo, xDeclare));
        Assert.False(variableAccessMap.HasExclusiveWriteAccess(bar, xDeclare));
    }

    [Fact]
    public void BuiltMap_handles_PointerAssignment()
    {
        // var a : *Int; 
        // fun f() : Unit {
        //   a = new(3);
        // }
        var tree = Program(
            Var("a", Pointer(new IntType()), out var aDeclare),
            Fun<UnitType>("f").Body(
                Value("a", out var aValue).Assign(Alloc(Literal(3)))
            ).Get(out var fun)
        );
        var nameResolution = NameResolution().WithVars(
            (aValue, aDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics);

        Assert.Contains((aDeclare, VariableAccessMode.WriteAndRead), variableAccessMap[fun]);
    }

    [Fact]
    public void BuiltMap_handles_PointerValueAssignment()
    {
        // var a = new(3); 
        // fun f() : Unit {
        //   *a = 2;
        // }
        var tree = Program(
            Var("a", Alloc(Literal(3)), out var aDeclare),
            Fun<UnitType>("f").Body(
                Deref(Value("a", out var aValue)).Assign(Literal(3))
            ).Get(out var fun)
        );
        var nameResolution = NameResolution().WithVars(
            (aValue, aDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics);

        // we did not modify the variable
        Assert.Contains((aDeclare, VariableAccessMode.ReadOnly), variableAccessMap[fun]);
    }

    [Fact]
    public void BuiltMap_handles_PointerDeref()
    {
        // var a = new(3); 
        // fun f() : Unit {
        //   var b = *a;
        // }
        var tree = Program(
            Var("a", Alloc(Literal(3)), out var aDeclare),
            Fun<UnitType>("f").Body(
                Var("b", Deref(Value("a", out var valueA)))
            ).Get(out var fun)
        );
        var nameResolution = NameResolution().WithVars(
            (valueA, aDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics);

        Assert.Contains((aDeclare, VariableAccessMode.ReadOnly), variableAccessMap[fun]);
    }

    [Fact]
    public void BuiltMap_handles_StructAssignment()
    {
        // struct TestStruct {
        //   field : Int
        // }
        // var a = TestStruct { field : 2 };
        // fun f() {
        //   a.field = 1;
        // }
        var tree = Program(
            Struct("TestStruct").Field("field", new IntType()),
            Var("a", StructValue("TestStruct").Field("field", Literal(2)), out var aDeclare),
            Fun<UnitType>("f").Body(
                Value("a", out var aValue).Field("field").Assign(Literal(1))
            ).Get(out var fun)
        );
        var nameResolution = NameResolution().WithVars(
            (aValue, aDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics);

        Assert.Contains((aDeclare, VariableAccessMode.WriteAndRead), variableAccessMap[fun]);
    }

    [Fact]
    public void BuiltMap_handles_StructUse()
    {
        // struct TestStruct {
        //   field : Int
        // }
        // var a = TestStruct { field : 2 };
        // fun f() {
        //   var b = a.field;
        // }
        var tree = Program(
            Struct("TestStruct").Field("field", new IntType()),
            Var("a", StructValue("TestStruct").Field("field", Literal(2)), out var aDeclare),
            Fun<UnitType>("f").Body(
                Var("b").Assign(Value("a", out var aValue).Field("field"))
            ).Get(out var fun)
        );
        var nameResolution = NameResolution().WithVars(
            (aValue, aDeclare));
        var diagnostics = new FakeDiagnostics();
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics);

        Assert.Contains((aDeclare, VariableAccessMode.ReadOnly), variableAccessMap[fun]);
    }

    [Fact]
    public void InnerFunctionConstAssignment_Case1()
    {
        // const x;
        // fun foo() { x = 1; }
        var ast = Program(
            Const("x", out var xDeclare),
            Fun<UnitType>("foo")
                .Body(Value("x", out var xAssign1).Assign(Literal(1)))
                .Get(out var foo)
        );
        var nameResolution = NameResolution().WithVars((xAssign1, xDeclare));
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
                        .Body(Value("x", out var xAssign1).Assign(Literal(1)))
                        .Get(out var bar))
                .Get(out var foo)
        );
        var nameResolution = NameResolution().WithVars((xAssign1, xDeclare));
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
                                .Body(Value("x", out var xAssign1).Assign(Literal(1)))
                                .Get(out var c)
                        )
                )
        );
        var nameResolution = NameResolution().WithVars((xAssign1, xDeclare));
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
                            Value("y", out var yAssign1).Assign(Literal(1)),
                            Fun<UnitType>("c")
                                .Body(Value("x", out var xAssign1).Assign(Literal(1)))
                                .Get(out var c)
                        )
                        .Get(out var b)
                )
                .Get(out var a)
        );
        var nameResolution = NameResolution().WithVars((xAssign1, xDeclare), (yAssign1, yDeclare));
        var diagnostics = new FakeDiagnostics();
        VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);

        var xError = new InnerFunctionConstVariableWriteError(ast, xDeclare, c, xAssign1);
        var yError = new InnerFunctionConstVariableWriteError(a, yDeclare, b, yAssign1);

        Assert.Equal(2, diagnostics.DiagnosticItems.LongCount());
        Assert.Contains(xError, diagnostics.DiagnosticItems);
        Assert.Contains(yError, diagnostics.DiagnosticItems);
    }
}
