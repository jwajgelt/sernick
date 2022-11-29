namespace sernick.CodeGeneration.InstructionSelection;

public sealed record CodeTreePatternRule(
    CodeTreePattern Pattern,
    CodeTreePatternRule.GenerateInstructionsDelegate GenerateInstructions)
{
    public delegate IEnumerable<IInstruction> GenerateInstructionsDelegate(IReadOnlyDictionary<CodeTreePattern, object> values);
}
