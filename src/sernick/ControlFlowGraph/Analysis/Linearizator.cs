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
        return dfs(root);
    }

    public IEnumerable<IAsmable> dfs(CodeTreeRoot? v)
    {
        if(v == null) { return new List<IAsmable>(); }
        if (v is SingleExitNode)
        {
            var node = (SingleExitNode)v;
            if (node.NextTree == null)
            {
                return _instructionCovering.Cover(node, null);
            }
            var nextTree = node.NextTree;
            var nextTreeCover = dfs(nextTree);
            var labelForNextTree = new Label("nextTree");
            // TODO 1 -- should we care about colliding labels?
            // TODO 2 -- how to attribute this label to a NextTree?
            var nodeCover = _instructionCovering.Cover(node, labelForNextTree);
            return nodeCover.Concat(nextTreeCover);
        }
        else
        {
            var conditionalNode = (ConditionalJumpNode)v;
            var trueCaseCover = dfs(conditionalNode.TrueCase);
            var falseCaseCover = dfs(conditionalNode.FalseCase);
            var trueCaseLabel = new Label("trueCase"); // TODO  -- how to attribute this label to a trueCase tree?
            var falseCaseLabel = new Label("falseCase"); // TODO  -- how to attribute this label to a falseCase tree?
            var conditionalNodeCover = _instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
            return conditionalNodeCover.Concat(trueCaseCover).Concat(falseCaseCover);
        }
    }
}
