namespace sernick.CodeGeneration.InstructionSelection;

using ControlFlowGraph.CodeTree;

public sealed record CodeTreePatternRule(
    CodeTreePattern Pattern,
    CodeTreePatternRule.GenerateInstructionsDelegate GenerateInstructions)
{
    public delegate IEnumerable<IInstruction> GenerateInstructionsDelegate(
        IReadOnlyList<Register> inputs,
        IReadOnlyDictionary<CodeTreePattern, object> values);
}
