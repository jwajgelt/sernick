namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeGeneration.InstructionSelection;
using CodeTree;

public class InstructionCovering
{
    public InstructionCovering(IReadOnlyList<CodeTreePatternRule> rules)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<IInstruction> Cover(SingleExitNode node, Label next)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<IInstruction> Cover(ConditionalJumpNode node, Label trueCase, Label falseCase)
    {
        throw new NotImplementedException();
    }
}
