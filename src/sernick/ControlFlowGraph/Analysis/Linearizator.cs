namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeTree;

public class Linearizator
{
    private readonly InstructionCovering _instructionCovering;

    public Linearizator(InstructionCovering instructionCovering)
    {
        _instructionCovering = instructionCovering;
    }

    public IReadOnlyList<IAsmable> Linearize(CodeTreeRoot root)
    {
        throw new NotImplementedException();
    }
}
