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
    public abstract string ToAsm();
}

public sealed record CallInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override string ToAsm()
    {
        throw new NotImplementedException();
    }
}

public sealed record JmpInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override string ToAsm()
    {
        throw new NotImplementedException();
    }
}

public sealed record RetInstruction : IInstruction
{
    public IEnumerable<Register> RegistersDefined => Enumerable.Empty<Register>();

    public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public bool PossibleFollow => false;

    public Label? PossibleJump => null;

    public bool IsCopy => false;
    public string ToAsm()
    {
        throw new NotImplementedException();
    }
}
