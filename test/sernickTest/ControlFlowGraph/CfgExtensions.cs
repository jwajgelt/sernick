namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph.CodeTree;

public static class CfgExtensions
{
    /// <summary>
    /// Determines whether the CFGs starting in given CodeTreeRoots are isomorphic.
    /// </summary>
    public static bool IsIsomorphicWithCfg(this CodeTreeRoot root, CodeTreeRoot other)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Determines whether the CodeTrees starting in given CodeTreeNodes are isomorphic.
    /// Only refers to those trees, if a node is CodeTreeRoot, it ignores the links.
    /// </summary>
    public static bool IsIsomorphicWithCodeTree(this CodeTreeNode root, CodeTreeNode other)
    {
        throw new NotImplementedException();
    }
}
