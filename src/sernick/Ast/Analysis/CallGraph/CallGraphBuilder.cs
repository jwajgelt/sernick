namespace sernick.Ast.Analysis.CallGraph;

using NameResolution;
using Nodes;

/// <summary>
///     Class used to represent the call graph.
/// </summary>
public record struct CallGraph(
    IReadOnlyDictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> Graph
)
{
    public CallGraph JoinWith(CallGraph other)
    {
        return new CallGraph(
            Graph.Concat(other.Graph)
            .GroupBy(kv => kv.Key, kv => kv.Value)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(x => x).Distinct(),
                ReferenceEqualityComparer.Instance as IEqualityComparer<FunctionDefinition>
            )
        );
    }
};

/// <summary>
///     Static class with Process method, which extracts the call graph from a given AST.
///     Wraps CallGraphVisitor.
/// </summary>
public static class CallGraphBuilder
{
    public static CallGraph Process(AstNode ast, NameResolutionResult nameResolution)
    {
        var visitor = new CallGraphVisitor(nameResolution.CalledFunctionDeclarations);
        return visitor.VisitAstTree(ast, null);
    }

    /// <summary>
    ///     Visitor class used to extract call graph from the AST.
    /// </summary>
    private sealed class CallGraphVisitor : AstVisitor<CallGraph, FunctionDefinition?>
    {
        private readonly IReadOnlyDictionary<FunctionCall, FunctionDefinition> _calledFunctionDeclarations;

        public CallGraphVisitor(IReadOnlyDictionary<FunctionCall, FunctionDefinition> calledFunctionDeclarations)
        {
            _calledFunctionDeclarations = calledFunctionDeclarations;
        }

        protected override CallGraph VisitAstNode(AstNode node, FunctionDefinition? fun)
        {
            return node.Children.Aggregate(
                new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>>()),
                (result, next) =>
                {
                    var childResult = next.Accept(this, fun);
                    return result.JoinWith(childResult);
                }
            );
        }

        public override CallGraph VisitFunctionDefinition(FunctionDefinition node, FunctionDefinition? fun)
        {
            var graph = VisitAstNode(node, node);

            var newDefDict = new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
                { node, new List<FunctionDefinition> { } }
            };

            return graph.JoinWith(new CallGraph(newDefDict));
        }

        public override CallGraph VisitFunctionCall(FunctionCall node, FunctionDefinition? fun)
        {
            var graph = VisitAstNode(node, fun);
            if (fun is not null)
            {
                var newCallDict = new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> {
                    { fun, new List<FunctionDefinition> { _calledFunctionDeclarations[node] } }
                };
                graph = graph.JoinWith(new CallGraph(newCallDict));
            }

            return graph;
        }
    }
}
