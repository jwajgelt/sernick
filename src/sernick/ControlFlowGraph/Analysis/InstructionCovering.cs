namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeGeneration.InstructionSelection;
using CodeTree;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternRuleMatchExtensions;

public sealed class InstructionCovering
{
    private record TreeCoverResult(uint Cost, IReadOnlyCollection<CodeTreeNode> Leaves, GenerateInstructions Generator);

    private record SingleExitCoverResult(uint Cost, IReadOnlyCollection<CodeTreeNode> Leaves, GenerateSingleExitInstructions Generator);

    private record ConditionalJumpCoverResult(uint Cost, IReadOnlyCollection<CodeTreeNode> Leaves, GenerateConditionalJumpInstructions Generator);

    private readonly IEnumerable<CodeTreePatternRule> _rules;
    private readonly Dictionary<CodeTreeNode, TreeCoverResult?> _resMemoizer;
    public InstructionCovering(IEnumerable<CodeTreePatternRule> rules)
    {
        _rules = rules.ToList();
        _resMemoizer = new Dictionary<CodeTreeNode, TreeCoverResult?>(ReferenceEqualityComparer.Instance);
    }

    private TreeCoverResult? CoverTree(CodeTreeNode node)
    {
        if (_resMemoizer.TryGetValue(node, out var result))
        {
            return result;
        }

        TreeCoverResult? best = null;
        foreach (var patternRule in _rules)
        {
            if (patternRule.TryMatchCodeTreeNode(node,
                out var leaves,
                out var generateInstructions
                ))
            {
                var leavesList = leaves.ToList();
                var cost = 1 + LeavesCost(leavesList);
                if (cost != null && (best == null || cost < best.Cost))
                {
                    best = new TreeCoverResult(cost.GetValueOrDefault(), leavesList, generateInstructions);
                }
            }
        }

        _resMemoizer[node] = best;
        return best;
    }

    public IEnumerable<IInstruction> Cover(SingleExitNode node, Label next)
    {
        SingleExitCoverResult? best = null;
        foreach (var patternRule in _rules)
        {
            if (patternRule.TryMatchSingleExitNode(node,
                out var leaves,
                out var generateInstructions
                ))
            {
                var leavesList = leaves.ToList();
                var cost = 1 + LeavesCost(leavesList);
                
                if (cost != null && (best == null || cost < best.Cost))
                {
                    best = new SingleExitCoverResult(cost.GetValueOrDefault(), leavesList, generateInstructions);
                }
            }
        }

        if (best is null)
        {
            throw new Exception("Unable to cover with given covering rules set.");
        }

        return GenerateSingleExitCovering(best, next);
    }

    public IEnumerable<IInstruction> Cover(ConditionalJumpNode node, Label trueCase, Label falseCase)
    {
        ConditionalJumpCoverResult? best = null;
        foreach (var patternRule in _rules)
        {
            if (patternRule.TryMatchConditionalJumpNode(node,
                out var leaves,
                out var generateInstructions
                ))
            {
                var leavesList = leaves.ToList();
                var cost = 1 + LeavesCost(leavesList);
                if (cost != null && (best == null || cost < best.Cost))
                {
                    best = new ConditionalJumpCoverResult(cost.GetValueOrDefault(), leavesList, generateInstructions);
                }
            }
        }

        if (best is null)
        {
            throw new Exception("Unable to cover with given covering rules set.");
        }

        return GenerateConditionalJumpCovering(best, trueCase, falseCase);
    }

    private uint? LeavesCost(IEnumerable<CodeTreeNode> leaves)
    {
        return leaves
            .Select(CoverTree)
            .Aggregate(0u as uint?, (current, leafCover) => current + leafCover?.Cost);
    }

    private IEnumerable<IInstruction> GenerateCovering(TreeCoverResult result, out Register? output)
    {
        var instructions = new List<IInstruction>();
        var leafOutputs = new List<Register>();
        foreach (var leaf in result.Leaves)
        {
            var leafCover = CoverTree(leaf);
            if (leafCover is null)
            {
                continue;
            }

            instructions.AddRange(GenerateCovering(leafCover, out var leafOutput));

            if (leafOutput is not null)
            {
                leafOutputs.Add(leafOutput);
            }
        }

        instructions.AddRange(result.Generator(leafOutputs, out output));

        return instructions;
    }

    private IEnumerable<IInstruction> GenerateSingleExitCovering(SingleExitCoverResult result, Label next)
    {
        var instructions = new List<IInstruction>();

        foreach (var leaf in result.Leaves)
        {
            var leafCover = CoverTree(leaf);
            if (leafCover is null)
            {
                continue;
            }

            instructions.AddRange(GenerateCovering(leafCover, out _));
        }

        instructions.AddRange(result.Generator(next));
        return instructions;
    }

    private IEnumerable<IInstruction> GenerateConditionalJumpCovering(ConditionalJumpCoverResult result, Label trueCase, Label falseCase)
    {
        var instructions = new List<IInstruction>();
        if (result.Leaves.Count != 1)
        {
            throw new Exception("Conditional jump should have exactly one leaf.");
        }

        var condition = result.Leaves.First();
        var conditionCover = CoverTree(condition);

        if (conditionCover is null)
        {
            throw new Exception("Condition should be coverable.");
        }

        instructions.AddRange(GenerateCovering(conditionCover, out var conditionOutput));

        if (conditionOutput is null)
        {
            throw new Exception("Condition should have output.");
        }

        instructions.AddRange(result.Generator(conditionOutput, trueCase, falseCase));

        return instructions;
    }
}
