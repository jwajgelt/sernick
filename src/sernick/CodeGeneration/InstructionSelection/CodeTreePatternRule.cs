namespace sernick.CodeGeneration.InstructionSelection;

using ControlFlowGraph.CodeTree;

public sealed record CodeTreePatternRule(
    CodeTreePattern Pattern,
    CodeTreePatternRule.GenerateInstructionsDelegate GenerateInstructions)
{
    public delegate (IEnumerable<IInstruction> instructions, Register? output) GenerateInstructionsDelegate(
        IReadOnlyList<Register> inputs,
        IReadOnlyDictionary<CodeTreePattern, object> values);
}
