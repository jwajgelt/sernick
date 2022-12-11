namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeGeneration.InstructionSelection;
using CodeTree;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternRuleMatchExtensions;

public sealed class InstructionCovering
{
    private record TreeCoverResult(int Cost, IEnumerable<CodeTreeNode> Leaves, GenerateInstructions? Generator);

    private record SingleExitCoverResult(int Cost, IEnumerable<CodeTreeNode> Leaves, GenerateSingleExitInstructions? Generator);

    private record ConditionalJumpCoverResult(int Cost, IEnumerable<CodeTreeNode> Leaves, GenerateConditionalJumpInstructions? Generator);

    private readonly IEnumerable<CodeTreePatternRule> _rules;
    private readonly Dictionary<CodeTreeNode, TreeCoverResult?> _resMemoizer;
    public InstructionCovering(IEnumerable<CodeTreePatternRule> rules)
    {
        _rules = rules;
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
                var cost = 1 + LeavesCost(leaves);
                if (cost != null && (best == null || cost < best.Cost))
                {
                    best = new TreeCoverResult(cost.GetValueOrDefault(), leaves, generateInstructions);
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
                var cost = 1 + LeavesCost(leaves);
                if (cost != null && (best == null || cost < best.Cost))
                {
                    best = new SingleExitCoverResult(cost.GetValueOrDefault(), leaves, generateInstructions);
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
                var cost = 1 + LeavesCost(leaves);
                if (cost != null && (best == null || cost < best.Cost))
                {
                    best = new ConditionalJumpCoverResult(cost.GetValueOrDefault(), leaves, generateInstructions);
                }
            }
        }

        if (best is null)
        {
            throw new Exception("Unable to cover with given covering rules set.");
        }

        return GenerateConditionalJumpCovering(best, trueCase, falseCase);
    }

    private int? LeavesCost(IEnumerable<CodeTreeNode>? leaves)
    {
        var cost = 0;
        if (leaves is not null)
        {
            foreach (var leaf in leaves)
            {
                var leafCover = CoverTree(leaf);
                if (leafCover is null)
                {
                    return null;
                }

                cost += leafCover.Cost;
            }
        }

        return cost;
    }

    private IEnumerable<IInstruction> GenerateCovering(TreeCoverResult result, out Register? output)
    {
        var instructions = new List<IInstruction>();
        var leafOutputs = new List<Register>();
        if (result.Leaves is not null)
        {
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
        }

        output = null;
        if (result.Generator is not null)
        {
            instructions.AddRange(result.Generator(leafOutputs, out var genOutput));
            output = genOutput;
        }

        return instructions;
    }

    private IEnumerable<IInstruction> GenerateSingleExitCovering(SingleExitCoverResult result, Label next)
    {
        var instructions = new List<IInstruction>();
        if (result.Leaves is not null)
        {
            foreach (var leaf in result.Leaves)
            {
                var leafCover = CoverTree(leaf);
                if (leafCover is null)
                {
                    continue;
                }

                instructions.AddRange(GenerateCovering(leafCover, out _));
            }
        }

        if (result.Generator is not null)
        {
            instructions.AddRange(result.Generator(next));
        }

        return instructions;
    }

    private IEnumerable<IInstruction> GenerateConditionalJumpCovering(ConditionalJumpCoverResult result, Label trueCase, Label falseCase)
    {
        var instructions = new List<IInstruction>();
        if (result.Leaves is null || result.Leaves.Count() != 1)
        {
            throw new Exception("Conditional jump should have exactly one leaf.");
        }

        var condition = result.Leaves.ElementAt(0);
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

        if (result.Generator is not null)
        {
            instructions.AddRange(result.Generator(conditionOutput, trueCase, falseCase));
        }

        return instructions;
    }
}
