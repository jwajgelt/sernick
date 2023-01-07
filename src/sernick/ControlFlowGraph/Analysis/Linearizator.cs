namespace sernick.ControlFlowGraph.Analysis;

using System.Diagnostics;
using CodeGeneration;
using CodeTree;
using Utility;

public sealed class Linearizator
{
    private readonly IInstructionCovering _instructionCovering;
    private readonly ISet<CodeTreeRoot> _visited;
    private readonly LabelGenerator _labelGenerator;
    private IReadOnlySet<CodeTreeRoot> _requiresLabel;

    public Linearizator(IInstructionCovering instructionCovering)
    {
        _instructionCovering = instructionCovering;
        _visited = new HashSet<CodeTreeRoot>(ReferenceEqualityComparer.Instance);
        _labelGenerator = new LabelGenerator();
        _requiresLabel = new HashSet<CodeTreeRoot>(ReferenceEqualityComparer.Instance);
    }

    public IEnumerable<IAsmable> Linearize(CodeTreeRoot root, Label startLabel)
    {
        _labelGenerator.SetStart(root, startLabel);
        _requiresLabel = CalculateRequiresLabel(root);
        _visited.Clear();
        return Dfs(root, out _).asmables;
    }

    private (Label? label, IEnumerable<IAsmable> asmables) Dfs(CodeTreeRoot root, out bool processedBefore)
    {
        if (_visited.Contains(root))
        {
            processedBefore = true;
            return (_labelGenerator.GetLabel(root), Enumerable.Empty<IAsmable>());
        }

        processedBefore = false;
        _visited.Add(root);

        var label = _requiresLabel.Contains(root) ? _labelGenerator.GetLabel(root) : null;

        var asmables = root switch
        {
            SingleExitNode node => HandleSingleExitNode(node),
            ConditionalJumpNode conditionalNode => HandleConditionalJumpNode(conditionalNode),
            _ => throw new Exception(
                $"<Linearizator> called on a node which is neither a SingleExitNode nor ConditionalJumpNode : {root}")
        };

        return (label, label is null ? asmables : label.Enumerate().Concat(asmables));
    }

    private IEnumerable<IAsmable> HandleSingleExitNode(SingleExitNode node)
    {
        if (node.NextTree == null)
        {
            return _instructionCovering.Cover(node, null);
        }

        var (nextTreeLabel, nextTreeCoverWithLabel) = Dfs(node.NextTree, out var processedBefore);

        // if the nextTree wasn't processed before, then we will have it placed right after the code
        // for this node, so there is no need to generate a jump to nextTreeLabel at the end
        var nodeCover = processedBefore ?
            _instructionCovering.Cover(node, nextTreeLabel) : _instructionCovering.Cover(node, null);

        return nodeCover.Concat(nextTreeCoverWithLabel);
    }

    private IEnumerable<IAsmable> HandleConditionalJumpNode(ConditionalJumpNode conditionalNode)
    {
        var (trueCaseLabel, trueCaseCoverWithLabel) = Dfs(conditionalNode.TrueCase, out _);

        var (falseCaseLabel, falseCaseCoverWithLabel) = Dfs(conditionalNode.FalseCase, out _);

        Debug.Assert(trueCaseLabel is not null && falseCaseLabel is not null);

        var conditionalNodeCover = _instructionCovering.Cover(conditionalNode, trueCaseLabel, falseCaseLabel);

        // trueCaseCoverWithLabel and falseCaseCoverWithLabel are possibly empty lists, if those trees were already visited
        // so we can Concat(..) them here without any conditional checks
        return conditionalNodeCover.Concat(trueCaseCoverWithLabel).Concat(falseCaseCoverWithLabel);
    }

    /// <summary>
    /// Calculates all the nodes that require labels:
    /// <list type="bullet">
    ///     <item>
    ///     nodes that are children of conditional jumps
    ///     </item>
    ///     <item>
    ///     nodes that have in degree higher than 1
    ///     </item>
    ///     <item>
    ///     the root
    ///     </item>
    /// </list>
    /// </summary>
    /// <returns></returns>
    private static IReadOnlySet<CodeTreeRoot> CalculateRequiresLabel(CodeTreeRoot root)
    {
        var inDegreeHigherThanOne = new HashSet<CodeTreeRoot>(ReferenceEqualityComparer.Instance);
        var conditionalNodeChildren = new HashSet<CodeTreeRoot>(ReferenceEqualityComparer.Instance);
        var visited = new HashSet<CodeTreeRoot>(ReferenceEqualityComparer.Instance);
        var result = new HashSet<CodeTreeRoot>(ReferenceEqualityComparer.Instance);

        void Dfs(CodeTreeRoot node)
        {
            if (visited.Contains(node))
            {
                inDegreeHigherThanOne.Add(node);
                return;
            }

            visited.Add(node);

            switch (node)
            {
                case SingleExitNode singleExitNode:
                    if (singleExitNode.NextTree is not null)
                    {
                        Dfs(singleExitNode.NextTree);
                    }

                    break;
                case ConditionalJumpNode conditionalJumpNode:
                    conditionalNodeChildren.Add(conditionalJumpNode.FalseCase);
                    conditionalNodeChildren.Add(conditionalJumpNode.TrueCase);
                    Dfs(conditionalJumpNode.FalseCase);
                    Dfs(conditionalJumpNode.TrueCase);
                    break;
            }
        }

        Dfs(root);

        result.UnionWith(inDegreeHigherThanOne);
        result.UnionWith(conditionalNodeChildren);
        result.Add(root);
        return result;
    }

    private sealed class LabelGenerator
    {
        private CodeTreeRoot? _root;
        private int _order;
        private readonly Dictionary<CodeTreeRoot, Label> _cache = new(ReferenceEqualityComparer.Instance);

        public void SetStart(CodeTreeRoot root, Label startLabel)
        {
            _root = root;
            _order = 0;
            _cache.Clear();
            _cache[_root] = startLabel;
        }
        /// <summary>
        /// Returns a memorized Label for an already visited node or a new one otherwise.
        /// </summary>
        /// <returns></returns>
        public Label GetLabel(CodeTreeRoot node)
        {
            if (_cache.TryGetValue(node, out var label))
            {
                return label;
            }

            _order++;

            if (_root is null || !_cache.TryGetValue(_root, out var rootLabel))
            {
                throw new Exception("LabelGenerator was not properly initialized with SetStart");
            }

            label = $"{rootLabel.Value}_{_order}";
            _cache[node] = label;

            return label;
        }
    }
}
