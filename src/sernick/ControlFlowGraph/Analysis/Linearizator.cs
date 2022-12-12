namespace sernick.ControlFlowGraph.Analysis;

using System.Xml.Linq;
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
        var nextTree = node.NextTree;
        if (_visitedRootsLabels.TryGetValue(node.NextTree, out var nextTreeLabel))
        {
            return _instructionCovering.Cover(node, nextTreeLabel);
        }
        else
        {
            var treeLabel = GenerateLabel(nextDepth);
            var nextTreeCover = Dfs(nextTree, nextDepth);
            return _instructionCovering.Cover(node, nextTreeLabel).Append<IAsmable>(treeLabel).Concat(nextTreeCover);
        }
    }

    private IEnumerable<IAsmable> HandleConditionalJumpNode(ConditionalJumpNode conditionalNode, int depth)
    {
        if (_visitedRootsLabels.TryGetValue(conditionalNode, out var label))
        {
            var emptyCover = new List<IAsmable>();
            return emptyCover;
        }

        var nextDepth = depth + 1;

        var trueCaseNode = conditionalNode.TrueCase;
        List<IAsmable> trueCaseCoverWithLabel;

        var falseCaseNode = conditionalNode.FalseCase;
        List<IAsmable> falseCaseCoverWithLabel;

        if (_visitedRootsLabels.TryGetValue(trueCaseNode, out var trueCaseLabel))
        {
            trueCaseCoverWithLabel = new List<IAsmable>();
        }
        else
        {
            var newLabel = GenerateLabel(nextDepth);
            trueCaseCoverWithLabel = (List<IAsmable>)new List<IAsmable>() { newLabel }.Concat(Dfs(trueCaseNode, nextDepth));
        }

        if (_visitedRootsLabels.TryGetValue(falseCaseNode, out var falseCaseLabel))
        {
            falseCaseCoverWithLabel = new List<IAsmable>();
        }
        else
        {
            var newLabel = GenerateLabel(nextDepth);
            falseCaseCoverWithLabel = (List<IAsmable>)new List<IAsmable>() { newLabel }.Concat(Dfs(falseCaseNode, nextDepth));
        }

        var conditionalNodeCover = _instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
        // trueCaseCoverWithLabel and falseCaseCoverWithLabel are possibly empty lists, if those trees were already visited
        // so we can Concat(..) them here without any conditional checks
        return conditionalNodeCover.Concat(trueCaseCoverWithLabel).Concat(falseCaseCoverWithLabel);
    }

}
