namespace sernick.Ast.Analysis.ControlFlowGraph;

using Nodes;
using sernick.ControlFlowGraph.CodeTree;

public static class ControlFlowAnalyzer
{
    public static IReadOnlyList<CodeTreeNode> UnravelControlFlow(AstNode root, Func<AstNode, IReadOnlyList<CodeTreeNode>> pullOutSideEffects)
    {
        throw new NotImplementedException();
    }
}
