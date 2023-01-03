namespace sernickTest.Ast.Analysis.CallGraph;

using sernick.Ast;
using sernick.Ast.Analysis.CallGraph;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using static Helpers.AstNodesExtensions;

public class CallGraphBuilderTest
{
    [Fact]
    public void TestSimple()
    {
        // fun f() {}
        // fun g() { f() }
        // g()
        var tree = Program(
            Fun<UnitType>("f").Body(Close).Get(out var f),
            Fun<UnitType>("g").Body(
                "f".Call(out var fCall)
            ).Get(out var g),
            "g".Call(out var gCall)
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            { fCall, f },
            { gCall, g }
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{ g } },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{ f } }
        });

        var expectedClosure = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{ f, g } },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{ f } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph, expectedClosure);
    }

    [Fact]
    public void TestControlFlow()
    {
        // fun f() {}
        // fun g() {}
        // fun h() { if(true) { f() } else { g() } }
        // if(false) { g() } else { h() }
        var tree = Program(
            Fun<UnitType>("f").Body(Close).Get(out var f),
            Fun<UnitType>("g").Body(Close).Get(out var g),
            Fun<UnitType>("h").Body(
                If(true)
                .Then("f".Call(out var fCall))
                .Else("g".Call(out var gCall1))
            ).Get(out var h),
            If(false)
                .Then("g".Call(out var gCall2))
                .Else("h".Call(out var hCall))
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            { fCall, f },
            { gCall1, g },
            { gCall2, g },
            { hCall, h }
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{ g, h } },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{} },
            { h, new FunctionDefinition[]{ f, g } }
        });

        var expectedClosure = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{ f, g, h } },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{} },
            { h, new FunctionDefinition[]{ f, g } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph, expectedClosure);
    }

    [Fact]
    public void TestNestedFunction()
    {
        // fun f() {}
        // fun g() {
        //   fun h() { g() }
        //   f()
        // }
        var tree = Program(
            Fun<UnitType>("f").Body(Close).Get(out var f),
            Fun<UnitType>("g").Body(
                Fun<UnitType>("h").Body(
                    "g".Call(out var gCall)
                ).Get(out var h),
                "f".Call(out var fCall)
            ).Get(out var g)
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            { fCall, f },
            { gCall, g }
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{} },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{ f } },
            { h, new FunctionDefinition[]{ g } }
        });

        var expectedClosure = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{} },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{ f } },
            { h, new FunctionDefinition[]{ f, g } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph, expectedClosure);
    }

    [Fact]
    public void TestCallInParameter()
    {
        // fun f(arg : Int) { arg }
        // fun g() { 1 }
        // fun h() { f(f(0)+g()) }
        var tree = Program(
            Fun<IntType>("f")
            .Parameter<IntType>("arg")
            .Body(
                Value("arg")
            ).Get(out var f),

            Fun<IntType>("g").Body(
                Return(1)
            ).Get(out var g),

            Fun<UnitType>("h").Body(
                Return("f".Call().Argument(
                    "f".Call().Argument(Literal(0)).Get(out var fCallInner)
                    .Plus("g".Call(out var gCall))
                ).Get(out var fCallOuter))
            ).Get(out var h)
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            { fCallInner, f },
            { fCallOuter, f },
            { gCall, g }
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{} },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{} },
            { h, new FunctionDefinition[]{ f, g } }
        });

        var expectedClosure = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { tree, new FunctionDefinition[]{} },
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{} },
            { h, new FunctionDefinition[]{ f, g } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph, expectedClosure);
    }

    private static void Verify(
        Expression tree,
        Dictionary<FunctionCall, FunctionDefinition> functionNameResolution,
        CallGraph expectedCallGraph,
        CallGraph expectedClosure)
    {
        var nameResolution = new NameResolutionResult(
            new Dictionary<VariableValue, Declaration> { },
            new Dictionary<Assignment, VariableDeclaration> { },
            functionNameResolution,
            new Dictionary<StructIdentifier, StructDeclaration> { });
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var callGraphClosure = callGraph.Closure();

        Assert.Equal(expectedCallGraph.Graph.Keys.OrderBy(a => a.Name.Name), callGraph.Graph.Keys.OrderBy(a => a.Name.Name));
        foreach (var (fun, called) in expectedCallGraph.Graph)
        {
            Assert.Equal(called.OrderBy(a => a.Name.Name), callGraph.Graph[fun].OrderBy(a => a.Name.Name));
        }

        Assert.Equal(expectedClosure.Graph.Keys.OrderBy(a => a.Name.Name), callGraphClosure.Graph.Keys.OrderBy(a => a.Name.Name));
        foreach (var (fun, called) in expectedClosure.Graph)
        {
            Assert.Equal(called.OrderBy(a => a.Name.Name), callGraphClosure.Graph[fun].OrderBy(a => a.Name.Name));
        }
    }
}
