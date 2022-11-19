namespace sernickTest.Ast.Analysis;

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
                .Body("bar".Call().Argument(Value("x", out var xVal)))
                .Get(out var foo)
        );
        var nameResolution = NameResolution().WithVars((xVal, xDeclare));
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);

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
                .Body("x".Assign(1, out var xAssign))
                .Get(out var foo)
            );
        var nameResolution = NameResolution().WithAssigns((xAssign, xDeclare));
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);

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
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);

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
                    "x".Assign(1, out var xAssign)
                    )
                .Get(out var foo)
        );
        var nameResolution = NameResolution()
            .WithVars((xVal, xDeclare))
            .WithAssigns((xAssign, xDeclare));
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);

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
                                .Body("z".Assign(1, out var zAssign))
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
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);

        Assert.Single(variableAccessMap[f1Def], item => item.Equals((xDeclare, VariableAccessMode.WriteAndRead)));
        Assert.Single(variableAccessMap[f2Def], item => item.Equals((yDeclare, VariableAccessMode.WriteAndRead)));
        Assert.Single(variableAccessMap[f3Def], item => item.Equals((zDeclare, VariableAccessMode.WriteAndRead)));
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
                    "x".Assign(1, out var xAssign2))
                .Get(out var foo),
            Fun<UnitType>("bar")
                .Body(Close)
                .Get(out var bar)
        );
        var nameResolution = NameResolution().WithAssigns(
            (xAssign1, xDeclare),
            (xAssign2, xDeclare));
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);

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
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution);

        Assert.False(variableAccessMap.HasExclusiveWriteAccess(foo, xDeclare));
        Assert.False(variableAccessMap.HasExclusiveWriteAccess(bar, xDeclare));
    }
}
