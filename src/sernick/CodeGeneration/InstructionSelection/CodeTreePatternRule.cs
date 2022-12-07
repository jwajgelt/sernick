namespace sernick.CodeGeneration.InstructionSelection;

using ControlFlowGraph.CodeTree;

public abstract record CodeTreePatternRule;

public sealed record CodeTreeNodePatternRule(
    CodeTreePattern Pattern,
    CodeTreeNodePatternRule.GenerateInstructionsDelegate GenerateInstructions) : CodeTreePatternRule
{
    public delegate (IEnumerable<IInstruction> instructions, Register? output) GenerateInstructionsDelegate(
        IReadOnlyList<Register> inputs,
        IReadOnlyDictionary<CodeTreePattern, object> values);
}

public sealed record ConditionalJumpNodePatternRule(
    ConditionalJumpNodePatternRule.GenerateInstructionsDelegate GenerateInstructions) : CodeTreePatternRule
{
    public delegate IEnumerable<IInstruction> GenerateInstructionsDelegate(
        Register input,
        Label trueCase,
        Label falseCase);
}

public sealed record SingleExitNodePatternRule(
    SingleExitNodePatternRule.GenerateInstructionsDelegate GenerateInstructions) : CodeTreePatternRule
{
    public delegate IEnumerable<IInstruction> GenerateInstructionsDelegate(Label next);
}
