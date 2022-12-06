namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeGeneration.InstructionSelection;
using CodeTree;

public sealed class InstructionCovering
{
    public InstructionCovering(IEnumerable<CodeTreePatternRule> rules)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IInstruction> Cover(SingleExitNode node, Label next)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IInstruction> Cover(ConditionalJumpNode node, Label trueCase, Label falseCase)
    {
        throw new NotImplementedException();
    }
}
