namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeTree;

public sealed class Linearizator
{
    private readonly InstructionCovering _instructionCovering;
    private Dictionary<CodeTreeRoot, Label> _visitedRootsLabels;

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
        var nextDepth = depth + 1;

        if (v is SingleExitNode node)
        {
            if (node.NextTree == null)
            {
                return _instructionCovering.Cover(node, null);
            }

            var nextTree = node.NextTree;
            var nextTreeCover = dfs(nextTree, nextDepth);
            var labelForNextTree = generateLabel(depth);
            _visitedRootsLabels[nextTree] = labelForNextTree;

            var nodeCover = (List<IAsmable>)_instructionCovering.Cover(node, labelForNextTree);
            return nodeCover.Append(labelForNextTree).Concat(nextTreeCover);
        }
        else
        {
            var conditionalNode = (ConditionalJumpNode)v;
            var trueCaseNode = conditionalNode.TrueCase;
            if(trueCaseNode == null)
            {
                throw new Exception("<Linearizator> Node " + v + " has TrueCase equal to null, but it should be non-nullable");
            }
            var trueCaseLabel = generateLabel(depth);
            var trueCaseCover = dfs(trueCaseNode, nextDepth);
            _visitedRootsLabels[trueCaseNode] = trueCaseLabel;

            var falseCaseNode = conditionalNode.FalseCase;
            if (falseCaseNode == null)
            {
                throw new Exception("<Linearizator> Node " + v + " has TrueCase equal to null, but it should be non-nullable");
            }
            var falseCaseLabel = generateLabel(depth);
            var falseCaseCover = dfs(falseCaseNode, nextDepth);
            _visitedRootsLabels[falseCaseNode] = falseCaseLabel;

            var conditionalNodeCover = (List<IAsmable>)_instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
            return conditionalNodeCover.Append(trueCaseLabel).Concat(trueCaseCover).Append(falseCaseLabel).Concat(falseCaseCover);
        }
    }
}
