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
        // f {}
        // g { f() }
        var tree = Program(
            Fun<UnitType>("f").Body(Close).Get(out var f),
            Fun<UnitType>("g").Body(
                "f".Call(out var fCall), Close
            ).Get(out var g)
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            {fCall, f}
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{ f } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph);
    }

    [Fact]
    public void TestControlFlow()
    {
        // f {}
        // g {}
        // h { if(true) { f() } else { g() } }
        var tree = Program(
            Fun<UnitType>("f").Body(Close).Get(out var f),
            Fun<UnitType>("g").Body(Close).Get(out var g),
            Fun<UnitType>("h").Body(
                If(Literal(true))
                .Then("f".Call(out var fCall), Close)
                .Else("g".Call(out var gCall), Close)
            ).Get(out var h)
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            {fCall, f},
            {gCall, g}
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{} },
            { h, new FunctionDefinition[]{ f,g } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph);
    }

    [Fact]
    public void TestNestedFunction()
    {
        // f {}
        // g {
        //   h { g() }
        //   f()
        // }
        var tree = Program(
            Fun<UnitType>("f").Body(Close).Get(out var f),
            Fun<UnitType>("g").Body(
                Fun<UnitType>("h").Body(
                    "g".Call(out var gCall), Close
                ).Get(out var h),
                "f".Call(out var fCall),
                Close
            ).Get(out var g)
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            {fCall, f},
            {gCall, g}
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{ f } },
            { h, new FunctionDefinition[]{ g } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph);
    }

    [Fact]
    public void TestCallInParameter()
    {
        // f(arg : Int) { arg }
        // g { 1 }
        // h { f(f(0)+g()) }
        var tree = Program(
            Fun<IntType>("f")
            .Parameter<IntType>("arg")
            .Body(
                Return(Value("arg")), Close
            ).Get(out var f),

            Fun<IntType>("g").Body(
                Return(1), Close
            ).Get(out var g),

            Fun<UnitType>("h").Body(
                Return("f".Call(new[]{
                    "f".Call(new[]{Literal(0)}, out var fCallInner)
                    .Plus("g".Call(out var gCall))
                }, out var fCallOuter)),
                Close
            ).Get(out var h)
        );

        var functionNameResolution = new Dictionary<FunctionCall, FunctionDefinition> {
            {fCallInner, f},
            {fCallOuter, f},
            {gCall, g}
        };

        var expectedCallGraph = new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
            { f, new FunctionDefinition[]{} },
            { g, new FunctionDefinition[]{} },
            { h, new FunctionDefinition[]{ f, g } }
        });

        Verify(tree, functionNameResolution, expectedCallGraph);
    }

    private static void Verify(
        Expression tree,
        Dictionary<FunctionCall, FunctionDefinition> functionNameResolution,
        CallGraph expectedCallGraph)
    {
        var nameResolution = new NameResolutionResult(
            new Dictionary<VariableValue, Declaration> { },
            new Dictionary<Assignment, VariableDeclaration> { },
            functionNameResolution);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);

        Assert.Equal(expectedCallGraph.Graph.Keys.OrderBy(a => a.Name.Name), callGraph.Graph.Keys.OrderBy(a => a.Name.Name));
        foreach (var (fun, called) in expectedCallGraph.Graph)
        {
            Assert.Equal(called.OrderBy(a => a.Name.Name), callGraph.Graph[fun].OrderBy(a => a.Name.Name));
        }
    }
}
