namespace sernick.CodeGeneration.InstructionSelection;

using System.Diagnostics.CodeAnalysis;
using ControlFlowGraph.CodeTree;

public class CodeTreePatternMatcher
{
    private readonly IReadOnlyCollection<CodeTreePatternRule> _rules;

    public CodeTreePatternMatcher(IEnumerable<CodeTreePatternRule> rules) => _rules = rules.ToList();

    /// <summary>
    /// Tries to match <see cref="root"/> onto each pattern, until succeeds. In that case,
    /// returns true and
    /// sets <see cref="leaves"/> to subtrees that were matched onto Wildcard nodes, and
    /// sets <see cref="GenerateInstructions"/> to a function which, given input registers,
    /// is able to generate a list of assembly instructions and an output register.
    /// </summary>
    public bool MatchCodeTree(CodeTreeNode root,
        [NotNullWhen(true)] out IEnumerable<CodeTreeNode>? leaves,
        [NotNullWhen(true)] out GenerateInstructions? generateInstructions)
    {
        foreach (var rule in _rules)
        {
            var values = new Dictionary<CodeTreePattern, object>(ReferenceEqualityComparer.Instance);
            if (rule.Pattern.TryMatch(root, out var matchedLeaves, values))
            {
                leaves = matchedLeaves;
                generateInstructions = (IReadOnlyList<Register> inputs, out Register? output) =>
                {
                    var (instructions, outputRegister) = rule.GenerateInstructions(inputs, values);
                    output = outputRegister;
                    return instructions;
                };
                return true;
            }
        }

        // shouldn't happen (perhaps only in tests)
        leaves = null;
        generateInstructions = null;
        return false;
    }

    public delegate IEnumerable<IInstruction> GenerateInstructions(IReadOnlyList<Register> inputs, out Register? output);
}
