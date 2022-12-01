namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph.CodeTree;

public sealed class CfgIsomorphismComparer : IEqualityComparer<CodeTreeRoot>
{
    private bool areEqual(
        CodeTreeRoot? x,
        CodeTreeRoot? y, 
        Dictionary<Register, int> regToLabel, 
        HashSet<CodeTreeRoot> visitSet
        )
    {
        if(x is null && y is null)
        {
            return true;
        }

        if(x is null || y is null)
        {
            return false;
        }

        if(visitSet.Contains(x) != visitSet.Contains(y))
        {
            return false;
        }

        if(visitSet.Contains(x))    // ???
        {
            return true;
        }

        visitSet.Add(x);
        visitSet.Add(y);
    }

    public bool Equals(CodeTreeRoot? x, CodeTreeRoot? y)
    {   
        var regToLabel = new Dictionary<Register, Register>();
        var visitSet = new HashSet<CodeTreeRoot>();

        return false;
    }

    public int GetHashCode(CodeTreeRoot obj)
    {
        throw new NotImplementedException();
    }
}
