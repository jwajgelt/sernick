namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeTree;

public sealed class Linearizator
{
    private readonly InstructionCovering _instructionCovering;
    private readonly Dictionary<CodeTreeRoot, Label> _visitedRootsLabels;

    public Linearizator(InstructionCovering instructionCovering)
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
        var enhancedGuid = $"{Guid.NewGuid()}{depth}";
        return new Label(enhancedGuid);
    }

    private IEnumerable<IAsmable> Dfs(CodeTreeRoot v, int depth)
    {
        switch (v)
        {
            case SingleExitNode node:
                {
                    return HandleSingleExitNode(node, depth);
                }
            case ConditionalJumpNode conditionalNode:
                {
                    return HandleConditionalJumpNode(conditionalNode, depth);
                }
            default:
                throw new Exception($"<Linearizator> called on a node which is neither a SingleExitNode nor ConditionalJumpNode : {v}");
        }
    }

    private IEnumerable<IAsmable> HandleSingleExitNode(SingleExitNode node, int depth)
    {
        if (node.NextTree == null)
        {
            return _instructionCovering.Cover(node, null);
        }

        var nextDepth = depth + 1;
        var (nextTreeLabel, nextTreeCover) = GetTreeLabelAndCover(node.NextTree, nextDepth);

        var nodeCover = _instructionCovering.Cover(node, nextTreeLabel);
        return nodeCover.Append<IAsmable>(nextTreeLabel).Concat(nextTreeCover);
    }

    private IEnumerable<IAsmable> HandleConditionalJumpNode(ConditionalJumpNode conditionalNode, int depth)
    {
        var nextDepth = depth + 1;
        var trueCaseNode = conditionalNode.TrueCase;
        if (trueCaseNode == null)
        {
            throw new Exception($"<Linearizator> Node {conditionalNode} has TrueCase equal to null, but it should be non-nullable");
        }

        var (trueCaseLabel, trueCaseCoverWithLabel) = GetTreeLabelAndCover(trueCaseNode, nextDepth);

        var falseCaseNode = conditionalNode.FalseCase;
        if (falseCaseNode == null)
        {
            throw new Exception("<Linearizator> Node " + conditionalNode + " has TrueCase equal to null, but it should be non-nullable");
        }

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
            // TODO should it be more like a conditional jump, not just a label? IDK how to do it with our API :|
            var emptyCover = new List<IAsmable>();
            return (label, emptyCover);
        }

        var treeLabel = GenerateLabel(depth);
        var treeCover = Dfs(tree, depth + 1).Append<IAsmable>(treeLabel);
        _visitedRootsLabels.Add(tree, treeLabel);
        return (treeLabel, treeCover);
    }
}
