namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeTree;
using Utility;

public sealed class Linearizator
{
    private readonly IInstructionCovering _instructionCovering;
    private readonly Dictionary<CodeTreeRoot, Label> _visitedRootsLabels;

    public Linearizator(IInstructionCovering instructionCovering)
    {
        _instructionCovering = instructionCovering;
        _visitedRootsLabels = new Dictionary<CodeTreeRoot, Label>(ReferenceEqualityComparer.Instance);
    }

    public IEnumerable<IAsmable> Linearize(CodeTreeRoot root)
    {
        return Dfs(root, 0);
    }

    private static Label GenerateLabel(int depth)
    {
        var enhancedGuid = $"l{Guid.NewGuid().ToString().Replace('-', '_')}{depth}";
        return new Label(enhancedGuid);
    }

    private IEnumerable<IAsmable> Dfs(CodeTreeRoot v, int depth)
    {
        return v switch
        {
            SingleExitNode node => HandleSingleExitNode(node, depth),
            ConditionalJumpNode conditionalNode => HandleConditionalJumpNode(conditionalNode, depth),
            _ => throw new Exception($"<Linearizator> called on a node which is neither a SingleExitNode nor ConditionalJumpNode : {v}")
        };
    }

    private IEnumerable<IAsmable> HandleSingleExitNode(SingleExitNode node, int depth)
    {
        if (node.NextTree == null)
        {
            return _instructionCovering.Cover(node, null);
        }

        var nextDepth = depth + 1;
        var (nextTreeLabel, nextTreeCoverWithLabel) = GetTreeLabelAndCover(node.NextTree, nextDepth);

        var nodeCover = _instructionCovering.Cover(node, nextTreeLabel);
        return nodeCover.Concat(nextTreeCoverWithLabel);
    }

    private IEnumerable<IAsmable> HandleConditionalJumpNode(ConditionalJumpNode conditionalNode, int depth)
    {
        var nextDepth = depth + 1;
        var trueCaseNode = conditionalNode.TrueCase;

        var (trueCaseLabel, trueCaseCoverWithLabel) = GetTreeLabelAndCover(trueCaseNode, nextDepth);

        var falseCaseNode = conditionalNode.FalseCase;

        var (falseCaseLabel, falseCaseCoverWithLabel) = GetTreeLabelAndCover(falseCaseNode, nextDepth);

        var conditionalNodeCover = _instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
        // trueCaseCoverWithLabel and falseCaseCoverWithLabel are possibly empty lists, if those trees were already visited
        // so we can Concat(..) them here without any conditional checks
        return conditionalNodeCover.Concat(trueCaseCoverWithLabel).Concat(falseCaseCoverWithLabel);
    }

    private ValueTuple<Label, IEnumerable<IAsmable>> GetTreeLabelAndCover(CodeTreeRoot tree, int depth)
    {
        if (_visitedRootsLabels.TryGetValue(tree, out var label))
        {
            var emptyCover = Enumerable.Empty<IAsmable>();
            return (label, emptyCover);
        }

        var treeLabel = GenerateLabel(depth);
        var treeCoverWithLabel = treeLabel.Enumerate().Concat(Dfs(tree, depth + 1));
        _visitedRootsLabels.Add(tree, treeLabel);
        return (treeLabel, treeCoverWithLabel);
    }
}
