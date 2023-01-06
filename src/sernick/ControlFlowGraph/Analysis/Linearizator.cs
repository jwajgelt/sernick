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
        return Dfs(root).asmables;
    }

    private (Label label, IEnumerable<IAsmable> asmables) Dfs(CodeTreeRoot root)
    {
        if (_visitedRootsLabels.TryGetValue(root, out var label))
        {
            return (label, Enumerable.Empty<IAsmable>());
        }

        label = _labelGenerator.GenerateLabel(root);
        _visitedRootsLabels.Add(root, label);

        var asmables = root switch
        {
            SingleExitNode node => HandleSingleExitNode(node),
            ConditionalJumpNode conditionalNode => HandleConditionalJumpNode(conditionalNode),
            _ => throw new Exception(
                $"<Linearizator> called on a node which is neither a SingleExitNode nor ConditionalJumpNode : {root}")
        };

        return (label, label.Enumerate().Concat(asmables));
    }

    private IEnumerable<IAsmable> HandleSingleExitNode(SingleExitNode node)
    {
        if (node.NextTree == null)
        {
            return _instructionCovering.Cover(node, null);
        }

        var (nextTreeLabel, nextTreeCoverWithLabel) = Dfs(node.NextTree);

        var nodeCover = _instructionCovering.Cover(node, nextTreeLabel);
        return nodeCover.Concat(nextTreeCoverWithLabel);
    }

    private IEnumerable<IAsmable> HandleConditionalJumpNode(ConditionalJumpNode conditionalNode)
    {
        var (trueCaseLabel, trueCaseCoverWithLabel) = Dfs(conditionalNode.TrueCase);

        var (falseCaseLabel, falseCaseCoverWithLabel) = Dfs(conditionalNode.FalseCase);

        var conditionalNodeCover = _instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);

        // trueCaseCoverWithLabel and falseCaseCoverWithLabel are possibly empty lists, if those trees were already visited
        // so we can Concat(..) them here without any conditional checks
        return conditionalNodeCover.Concat(trueCaseCoverWithLabel).Concat(falseCaseCoverWithLabel);
    }

    private sealed class LabelGenerator
    {
        private Label? _startLabel;
        private CodeTreeRoot? _root;
        private int _order;

        public void SetStart(CodeTreeRoot root, Label startLabel)
        {
            _startLabel = startLabel;
            _root = root;
            _order = 0;
        }
        public Label GenerateLabel(CodeTreeRoot root)
        {
            if (ReferenceEquals(root, _root))
            {
                return _startLabel!;
            }

            _order++;
            return _startLabel is null ?
                $"l{Guid.NewGuid().ToString().Replace('-', '_')}{_order}" :
                $"{_startLabel.Value}_{_order}";
        }
    }
}
