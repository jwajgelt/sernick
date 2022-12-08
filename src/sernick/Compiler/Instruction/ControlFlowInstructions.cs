namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public abstract record TransferControlInstruction(Label Location) : IInstruction
{
    public IEnumerable<Register> RegistersDefined => Enumerable.Empty<Register>();

    public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public bool PossibleFollow => false;

    public Label PossibleJump => Location;

    public bool IsCopy => false;
}

public sealed record CallInstruction(Label Location) : TransferControlInstruction(Location);

public sealed record JmpInstruction(Label Location) : TransferControlInstruction(Location);
