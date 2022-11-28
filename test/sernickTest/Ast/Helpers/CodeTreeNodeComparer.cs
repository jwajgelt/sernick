namespace sernickTest.Ast.Helpers;
using sernick.ControlFlowGraph.CodeTree;

public class CodeTreeNodeComparer : IEqualityComparer<CodeTreeNode>
{
    public bool Equals(CodeTreeNode? x, CodeTreeNode? y)
    {
        return x switch
        {
            null => y is null,
            ConditionalJumpNode => throw new NotImplementedException(),
            SingleExitNode xNode =>
                y is SingleExitNode yNode
                && xNode.Operations.SequenceEqual(yNode.Operations, this),
            _ => x.Equals(y)
        };
    }

    public int GetHashCode(CodeTreeNode obj)
    {
        throw new NotImplementedException();
    }
}
