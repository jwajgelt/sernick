namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeTree;
using Utility;

public sealed class Linearizator
{
    private readonly IInstructionCovering _instructionCovering;
    private readonly Dictionary<CodeTreeRoot, Label> _visitedRootsLabels;
    private readonly LabelGenerator _labelGenerator;

    public Linearizator(IInstructionCovering instructionCovering)
    {
        _instructionCovering = instructionCovering;
        _visitedRootsLabels = new Dictionary<CodeTreeRoot, Label>(ReferenceEqualityComparer.Instance);
        _labelGenerator = new LabelGenerator();
    }

    public IEnumerable<IAsmable> Linearize(CodeTreeRoot root, Label startLabel)
    {
        _labelGenerator.SetStart(root, startLabel);
        return Dfs(root, 0).asmables;
    }

    private (Label label, IEnumerable<IAsmable> asmables) Dfs(CodeTreeRoot root, int depth)
    {
        if (_visitedRootsLabels.TryGetValue(root, out var label))
        {
            return (label, Enumerable.Empty<IAsmable>());
        }

        label = _labelGenerator.GenerateLabel(root, depth);
        _visitedRootsLabels.Add(root, label);

        var asmables = root switch
        {
            SingleExitNode node => HandleSingleExitNode(node, depth),
            ConditionalJumpNode conditionalNode => HandleConditionalJumpNode(conditionalNode, depth),
            _ => throw new Exception(
                $"<Linearizator> called on a node which is neither a SingleExitNode nor ConditionalJumpNode : {root}")
        };

        return (label, label.Enumerate().Concat(asmables));
    }

    private IEnumerable<IAsmable> HandleSingleExitNode(SingleExitNode node, int depth)
    {
        if (node.NextTree == null)
        {
            return _instructionCovering.Cover(node, null);
        }

        var nextDepth = depth + 1;
        var (nextTreeLabel, nextTreeCoverWithLabel) = Dfs(node.NextTree, nextDepth);

        var nodeCover = _instructionCovering.Cover(node, nextTreeLabel);
        return nodeCover.Concat(nextTreeCoverWithLabel);
    }

    private IEnumerable<IAsmable> HandleConditionalJumpNode(ConditionalJumpNode conditionalNode, int depth)
    {
        var nextDepth = depth + 1;
        var trueCaseNode = conditionalNode.TrueCase;

        var (trueCaseLabel, trueCaseCoverWithLabel) = Dfs(trueCaseNode, nextDepth);

        var falseCaseNode = conditionalNode.FalseCase;

        var (falseCaseLabel, falseCaseCoverWithLabel) = Dfs(falseCaseNode, nextDepth);

        var conditionalNodeCover = _instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);
        // trueCaseCoverWithLabel and falseCaseCoverWithLabel are possibly empty lists, if those trees were already visited
        // so we can Concat(..) them here without any conditional checks
        return conditionalNodeCover.Concat(trueCaseCoverWithLabel).Concat(falseCaseCoverWithLabel);
    }

    private sealed class LabelGenerator
    {
        private Label? _startLabel;
        private CodeTreeRoot? _root;

        public void SetStart(CodeTreeRoot root, Label startLabel)
        {
            _startLabel = startLabel;
            _root = root;
        }
        public Label GenerateLabel(CodeTreeRoot root, int depth)
        {
            if (ReferenceEquals(root, _root))
            {
                return _startLabel!;
            }

            var enhancedGuid = $"l{Guid.NewGuid().ToString().Replace('-', '_')}{depth}";
            return new Label(enhancedGuid);
        }
    }
}
