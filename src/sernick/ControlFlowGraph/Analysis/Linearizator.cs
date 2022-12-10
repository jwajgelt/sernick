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
        _visitedRootsLabels = new Dictionary<CodeTreeRoot, Label>();
    }

    public IEnumerable<IAsmable> Linearize(CodeTreeRoot root)
    {
        return dfs(root, 0);
    }

    private static Label generateLabel(int depth)
    {
        // TODO labels should be unique on certain depth (and also unique overall)
        return new Label("TODO ME LATER");
    }

    private IEnumerable<IAsmable> dfs(CodeTreeRoot v, int depth)
    {
        if (v == null)
        {
            return new List<IAsmable>();
        }
        

        switch (v)
        {
            case SingleExitNode node:
                {
                    return handleSingleExitNode(node, depth);
                }
            case ConditionalJumpNode conditionalNode:
                {
                    return handleConditionalJumpNode(conditionalNode, depth);
                }
            default:
                return new List<IAsmable>(); // this should never happen :P
        }
    }

    private IEnumerable<IAsmable> handleSingleExitNode(SingleExitNode node, int depth)
    {
        if (node.NextTree == null)
        {
            return _instructionCovering.Cover(node, null);
        }

        var nextDepth = depth + 1;
        var (nextTreeLabel, nextTreeCover) = processTree(node.NextTree, nextDepth);

        var nodeCover = (List<IAsmable>)_instructionCovering.Cover(node, nextTreeLabel);
        return nodeCover.Append(nextTreeLabel).Concat(nextTreeCover);
    }

    private IEnumerable<IAsmable> handleConditionalJumpNode(ConditionalJumpNode conditionalNode, int depth)
    {
        var nextDepth = depth + 1;
        var trueCaseNode = conditionalNode.TrueCase;
        if (trueCaseNode == null)
        {
            throw new Exception("<Linearizator> Node " + conditionalNode + " has TrueCase equal to null, but it should be non-nullable");
        }
        var (trueCaseLabel, trueCaseCover) = processTree(trueCaseNode, nextDepth);

        var falseCaseNode = conditionalNode.FalseCase;
        if (falseCaseNode == null)
        {
            throw new Exception("<Linearizator> Node " + conditionalNode + " has TrueCase equal to null, but it should be non-nullable");
        }
        var (falseCaseLabel, falseCaseCover) = processTree(falseCaseNode, nextDepth);

        var conditionalNodeCover = (List<IAsmable>)_instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
        return conditionalNodeCover.Append(trueCaseLabel).Concat(trueCaseCover).Append(falseCaseLabel).Concat(falseCaseCover);
    }

    private ValueTuple<Label, IEnumerable<IAsmable>> processTree(CodeTreeRoot tree, int depth)
    {
        if (_visitedRootsLabels.ContainsKey(tree))
        {
            var label = _visitedRootsLabels[tree];
            var cover = new List<IAsmable>() { label };
            return (label, cover);
        }
        var treeLabel = generateLabel(depth);
        var treeCover = dfs(tree, depth + 1);
        _visitedRootsLabels.Add(tree, treeLabel);
        return ( treeLabel, treeCover );
    }
}
