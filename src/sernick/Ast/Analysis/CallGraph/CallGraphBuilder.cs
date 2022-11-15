namespace sernick.Ast.Analysis.CallGraph;

using sernick.Ast.Nodes;

/// <summary>
///     Class used to represent the call graph.
/// </summary>
public sealed record CallGraph(
    IReadOnlyDictionary<FunctionDefinition, IEnumerable<FunctionDefinition>> Graph
);

/// <summary>
///     Static class with Process method, which extracts the call graph from a given AST.
///     Wraps CallGraphVisitor.
/// </summary>
public static class CallGraphBuilder
{
    public static CallGraph Process(AstNode ast)
    {
        var visitor = new CallGraphVisitor();
        return visitor.VisitAstTree(ast, new CallGraphVisitorParam());
    }

    private sealed class CallGraphVisitorParam { }

    /// <summary>
    ///     Visitor class used to extract call graph from the AST.
    /// </summary>
    private sealed class CallGraphVisitor : AstVisitor<CallGraph, CallGraphVisitorParam>
    {
        protected override CallGraph VisitAstNode(AstNode node, CallGraphVisitorParam param)
        {
            throw new NotImplementedException();
        }
    }
}
