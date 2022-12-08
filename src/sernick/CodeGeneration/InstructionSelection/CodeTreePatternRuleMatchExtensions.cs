namespace sernick.CodeGeneration.InstructionSelection;

using System.Diagnostics.CodeAnalysis;
using ControlFlowGraph.CodeTree;

public static class CodeTreePatternRuleMatchExtensions
{
    /// <summary>
    /// Tries to match non-root CodeTreeNode <see cref="root"/> onto pattern <see cref="rule"/>. If succeeds,
    /// returns true and
    /// sets <see cref="leaves"/> to subtrees that were matched onto Wildcard nodes, and
    /// sets <see cref="GenerateInstructions"/> to a function which, given input registers,
    /// is able to generate a list of assembly instructions and an output register.
    /// </summary>
    public static bool TryMatchCodeTreeNode(this CodeTreePatternRule rule, CodeTreeNode root,
        [NotNullWhen(true)] out IEnumerable<CodeTreeNode>? leaves,
        [NotNullWhen(true)] out GenerateInstructions? generateInstructions)
    {
        leaves = null;
        generateInstructions = null;

        if (rule is not CodeTreeNodePatternRule nodeRule)
        {
            return false;
        }

        var values = new Dictionary<CodeTreePattern, object>(ReferenceEqualityComparer.Instance);
        if (nodeRule.Pattern.TryMatch(root, out var matchedLeaves, values))
        {
            leaves = matchedLeaves;
            generateInstructions = (IReadOnlyList<Register> inputs, out Register? output) =>
            {
                var (instructions, outputRegister) = nodeRule.GenerateInstructions(inputs, values);
                output = outputRegister;
                return instructions;
            };
            return true;
        }

        return false;
    }

    public delegate IEnumerable<IInstruction> GenerateInstructions(IReadOnlyList<Register> inputs, out Register? output);

    /// <summary>
    /// Tries to match SingleExitNode <see cref="root"/> onto pattern <see cref="rule"/>. If succeeds,
    /// returns true and
    /// sets <see cref="leaves"/> to subtrees that were matched onto Wildcard nodes, and
    /// sets <see cref="GenerateInstructions"/> to a function which, given label to next instruction,
    /// is able to generate a list of assembly instructions.
    /// </summary>
    public static bool TryMatchSingleExitNode(this CodeTreePatternRule rule, SingleExitNode root,
        [NotNullWhen(true)] out IEnumerable<CodeTreeNode>? leaves,
        [NotNullWhen(true)] out GenerateSingleExitInstructions? generateInstructions)
    {
        leaves = null;
        generateInstructions = null;

        if (rule is not SingleExitNodePatternRule singleExitRule)
        {
            return false;
        }

        if (SingleExitNodePattern.TryMatch(root, out var matchedLeaves))
        {
            leaves = matchedLeaves;
            generateInstructions = next => singleExitRule.GenerateInstructions(next);
            return true;
        }

        return false;
    }

    public delegate IEnumerable<IInstruction> GenerateSingleExitInstructions(Label next);

    /// <summary>
    /// Tries to match ConditionalJumpNode <see cref="root"/> onto pattern <see cref="rule"/>. If succeeds,
    /// returns true and
    /// sets <see cref="leaves"/> to subtrees that were matched onto Wildcard nodes, and
    /// sets <see cref="GenerateInstructions"/> to a function which, given input register and labels to true/false cases,
    /// is able to generate a list of assembly instructions.
    /// </summary>
    public static bool TryMatchConditionalJumpNode(this CodeTreePatternRule rule, ConditionalJumpNode root,
        [NotNullWhen(true)] out IEnumerable<CodeTreeNode>? leaves,
        [NotNullWhen(true)] out GenerateConditionalJumpInstructions? generateInstructions)
    {
        leaves = null;
        generateInstructions = null;

        if (rule is not ConditionalJumpNodePatternRule conditionalJumpRule)
        {
            return false;
        }

        if (ConditionalJumpNodePattern.TryMatch(root, out var matchedLeaves))
        {
            leaves = matchedLeaves;
            generateInstructions = (inputs, trueCase, falseCase) =>
                conditionalJumpRule.GenerateInstructions(inputs, trueCase, falseCase);
            return true;
        }

        return false;
    }

    public delegate IEnumerable<IInstruction> GenerateConditionalJumpInstructions(Register input,
        Label trueCase, Label falseCase);
}
