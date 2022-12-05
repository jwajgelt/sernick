namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph.CodeTree;

public class CfgComparison
{
    private readonly Dictionary<Register, int> _registerLabels;
    private int _labelsCount;
    private readonly Dictionary<CodeTreeRoot, CodeTreeRoot> _visitMap;
    public bool AreEqual { get; }

    public CfgComparison(CodeTreeRoot? x, CodeTreeRoot? y)
    {
        _registerLabels = new Dictionary<Register, int>(ReferenceEqualityComparer.Instance);
        _labelsCount = 0;
        _visitMap = new Dictionary<CodeTreeRoot, CodeTreeRoot>(ReferenceEqualityComparer.Instance);
        AreEqual = Same(x, y);
    }

    private bool Same(CodeTreeRoot? x, CodeTreeRoot? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        if (_visitMap.TryGetValue(x, out var visitX) && !ReferenceEquals(visitX, y))
        {
            return false;
        }

        if (_visitMap.TryGetValue(y, out var visitY) && !ReferenceEquals(visitY, x))
        {
            return false;
        }

        if (visitX is not null || visitY is not null)
        {
            return true;
        }

        _visitMap[x] = y;
        _visitMap[y] = x;

        return (x, y) switch
        {
            (SingleExitNode xSingle, SingleExitNode ySingle) =>
                Same(xSingle.Operations, ySingle.Operations) &&
                Same(xSingle.NextTree, ySingle.NextTree),
            (ConditionalJumpNode xConditional, ConditionalJumpNode yConditional) =>
                Same(xConditional.ConditionEvaluation, yConditional.ConditionEvaluation) &&
                Same(xConditional.TrueCase, yConditional.TrueCase) &&
                Same(xConditional.FalseCase, yConditional.FalseCase),
            _ => false,
        };
    }

    private bool Same(IReadOnlyList<CodeTreeNode> x, IReadOnlyList<CodeTreeNode> y)
    {
        return x.Count == y.Count && x.Zip(y).All(p => Same(p.First, p.Second));
    }

    private bool Same(CodeTreeNode x, CodeTreeNode y)
    {
        return (x, y) switch
        {
            (BinaryOperationNode xBinOp, BinaryOperationNode yBinOp) =>
                xBinOp.Operation.Equals(yBinOp.Operation) &&
                Same(xBinOp.Right, yBinOp.Right) &&
                Same(xBinOp.Left, yBinOp.Left),

            (UnaryOperationNode xUnOp, UnaryOperationNode yUnOp) =>
                xUnOp.Operation.Equals(yUnOp.Operation) &&
                Same(xUnOp.Operand, yUnOp.Operand),

            (MemoryRead xMemRd, MemoryRead yMemRd) =>
                Same(xMemRd.MemoryLocation, yMemRd.MemoryLocation),

            (MemoryWrite xMemWrt, MemoryWrite yMemWrt) =>
                Same(xMemWrt.MemoryLocation, yMemWrt.MemoryLocation) &&
                Same(xMemWrt.Value, yMemWrt.Value),

            (RegisterRead xRegRd, RegisterRead yRegRd) =>
                Same(xRegRd.Register, yRegRd.Register),

            (RegisterWrite xRegWrt, RegisterWrite yRegWrt) =>
                Same(xRegWrt.Register, yRegWrt.Register) &&
                Same(xRegWrt.Value, yRegWrt.Value),

            _ => x.Equals(y),
        };
    }

    private bool Same(Register x, Register y)
    {
        if (x is HardwareRegister && y is HardwareRegister)
        {
            return x.Equals(y);
        }

        if (x is HardwareRegister || y is HardwareRegister)
        {
            return false;
        }

        if (_registerLabels.ContainsKey(x) && _registerLabels.ContainsKey(y))
        {
            return _registerLabels[x] == _registerLabels[y];
        }

        if (_registerLabels.ContainsKey(x) || _registerLabels.ContainsKey(y))
        {
            return false;
        }

        _registerLabels[x] = _labelsCount;
        _registerLabels[y] = _labelsCount;
        _labelsCount += 1;
        return true;
    }
}

public sealed class CfgIsomorphismComparer : IEqualityComparer<CodeTreeRoot>
{
    public bool Equals(CodeTreeRoot? x, CodeTreeRoot? y)
    {
        var cfgComparison = new CfgComparison(x, y);
        return cfgComparison.AreEqual;
    }

    public int GetHashCode(CodeTreeRoot obj)
    {
        throw new NotImplementedException();
    }
}
