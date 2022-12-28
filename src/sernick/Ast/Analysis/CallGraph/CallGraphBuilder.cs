namespace sernick.Ast.Analysis.CallGraph;

using FunctionContextMap;
using NameResolution;
using Nodes;
using static ExternalFunctionsInfo;

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

    public CallGraph Closure()
    {
        var graph = Graph.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToList() as IEnumerable<FunctionDefinition>,
            ReferenceEqualityComparer.Instance as IEqualityComparer<FunctionDefinition>);

        var functions = Graph.Keys.ToList();
        foreach (var f in functions)
        {
            foreach (var g in functions)
            {
                foreach (var h in functions)
                {
                    if (!graph[g].Contains(h) && graph[g].Contains(f) && graph[f].Contains(h))
                    {
                        graph[g] = graph[g].Append(h);
                    }
                }
            }
        }

        return new CallGraph(graph);
    }

    public CallGraph ClosureWithinScope(FunctionDefinition enclosingFunction, FunctionContextMap functionContextMap)
    {
        var enclosingFunctionContext = functionContextMap[enclosingFunction];
        var enclosedFunctions = Graph.Keys.Where(function =>
        {
            var functionContext = functionContextMap[function];
            while (functionContext != null)
            {
                if (functionContext.ParentContext == enclosingFunctionContext)
                {
                    return true;
                }

                functionContext = functionContext.ParentContext;
            }

            return false;
        }).ToHashSet();

        var graph = enclosedFunctions.ToDictionary(function => function, _ => new HashSet<FunctionDefinition>());

        foreach (var f in enclosedFunctions)
        {
            foreach (var g in Graph[f])
            {
                if (enclosedFunctions.Contains(g))
                {
                    graph[f].Add(g);
                }
            }
        }

        return new CallGraph(graph.ToDictionary(
            kv => kv.Key,
            kv => kv.Value as IEnumerable<FunctionDefinition>
            )).Closure();
    }
}

/// <summary>
///     Static class with Process method, which extracts the call graph from a given AST.
///     Wraps CallGraphVisitor.
/// </summary>
public static class CallGraphBuilder
{
    public static CallGraph Process(AstNode ast, NameResolutionResult nameResolution)
    {
        var visitor = new CallGraphVisitor(nameResolution.CalledFunctionDeclarations);
        var callGraph = visitor.VisitAstTree(ast, (FunctionDefinition)ast);
        return ExternalFunctions
            .Select(funcInfo => funcInfo.Definition)
            .Intersect(nameResolution.CalledFunctionDeclarations.Values)
            .Select(funcDef =>
                new CallGraph(new Dictionary<FunctionDefinition, IEnumerable<FunctionDefinition>>
                {
                    [funcDef] = new List<FunctionDefinition>()
                }))
            .Aggregate(callGraph, (result, next) => result.JoinWith(next));
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
                { node, new List<FunctionDefinition>() }
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
