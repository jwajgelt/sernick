namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeTree;

public sealed class Linearizator
{
    private readonly InstructionCovering _instructionCovering;

    public Linearizator(InstructionCovering instructionCovering)
    {
        _instructionCovering = instructionCovering;
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

    private IEnumerable<IAsmable> dfs(CodeTreeRoot? v, int depth)
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
            var nodeCover = (List<IAsmable>)_instructionCovering.Cover(node, labelForNextTree);
            return nodeCover.Append(labelForNextTree).Concat(nextTreeCover);
        }
        else
        {
            var conditionalNode = (ConditionalJumpNode)v;
            var trueCaseCover = dfs(conditionalNode.TrueCase, nextDepth);
            var trueCaseLabel = generateLabel(depth);

            var falseCaseCover = dfs(conditionalNode.FalseCase, nextDepth);
            var falseCaseLabel = generateLabel(depth);

            var conditionalNodeCover = (List<IAsmable>)_instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
            return conditionalNodeCover.Append(trueCaseLabel).Concat(trueCaseCover).Append(falseCaseLabel).Concat(falseCaseCover);
        }
    }
}
