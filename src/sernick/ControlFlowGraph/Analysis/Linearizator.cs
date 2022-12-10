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
            var nextTree = node.NextTree;
            var nextTreeCover = dfs(nextTree, nextDepth);
            var labelForNextTree = generateLabel(depth);
            _visitedRootsLabels.Add(nextTree, labelForNextTree);

            var nodeCover = (List<IAsmable>)_instructionCovering.Cover(node, labelForNextTree);
            return nodeCover.Append(labelForNextTree).Concat(nextTreeCover);
        }

        private IEnumerable<IAsmable> handleConditionalJumpNode(ConditionalJumpNode conditionalNode, int depth)
        {
            var nextDepth = depth + 1;
            var trueCaseNode = conditionalNode.TrueCase;
            if (trueCaseNode == null)
            {
                throw new Exception("<Linearizator> Node " + conditionalNode + " has TrueCase equal to null, but it should be non-nullable");
            }
            var trueCaseLabel = generateLabel(depth);
            var trueCaseCover = dfs(trueCaseNode, nextDepth);
            _visitedRootsLabels.Add(trueCaseNode, trueCaseLabel);

            var falseCaseNode = conditionalNode.FalseCase;
            if (falseCaseNode == null)
            {
                throw new Exception("<Linearizator> Node " + conditionalNode + " has TrueCase equal to null, but it should be non-nullable");
            }
            var falseCaseLabel = generateLabel(depth);
            var falseCaseCover = dfs(falseCaseNode, nextDepth);
            _visitedRootsLabels.Add(falseCaseNode, falseCaseLabel);

            var conditionalNodeCover = (List<IAsmable>)_instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
            return conditionalNodeCover.Append(trueCaseLabel).Concat(trueCaseCover).Append(falseCaseLabel).Concat(falseCaseCover);

        }
}
