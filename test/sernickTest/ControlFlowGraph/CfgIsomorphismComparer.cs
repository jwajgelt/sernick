namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph.CodeTree;

sealed record RegisterLabel(int label);
sealed record LabelRead(RegisterLabel Register) : CodeTreeValueNode;
sealed record LabelWrite(RegisterLabel Register, CodeTreeValueNode Value) : CodeTreeNode;

static class CfgLabeler
{
    public static CodeTreeRoot LabelCfg(CodeTreeRoot original)
    {
        var regToLabel = new Dictionary<Register, int>();
        var visitSet = new HashSet<CodeTreeRoot>();

        // We want deep copy here
        var labeled = original;
        processRoot(regToLabel, visitSet, original, labeled);
        return labeled;
    }

    private static void processRoot(
        Dictionary<Register, int> regToLabel, 
        HashSet<CodeTreeRoot> visitSet,
        CodeTreeRoot original,
        CodeTreeRoot labeled
        )
    {

    }

    private static void processTree()
    {

    }
}

public sealed class CfgIsomorphismComparer : IEqualityComparer<CodeTreeRoot>
{
    public bool Equals(CodeTreeRoot? x, CodeTreeRoot? y)
    {   
        if(x is null && y is null)
        {
            return true;
        }

        if(x is null || y is null)
        {
            return false;
        }

        var labeledCfgX = CfgLabeler.LabelCfg(x);
        var labeledCfgY = CfgLabeler.LabelCfg(y);

        return labeledCfgX.Equals(labeledCfgY);
    }

    public int GetHashCode(CodeTreeRoot obj)
    {
        var labeledCfg = CfgLabeler.LabelCfg(obj);
        return labeledCfg.GetHashCode();
    }
}
