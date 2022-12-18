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

    public IInstruction ReplaceRegisters(IReadOnlyDictionary<Register, Register> defines,
        IReadOnlyDictionary<Register, Register> uses) =>
        this;

    public abstract string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping);
}

public sealed record CallInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        throw new NotImplementedException();
    }
}

public sealed record JmpInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
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

    public IInstruction ReplaceRegisters(IReadOnlyDictionary<Register, Register> defines,
        IReadOnlyDictionary<Register, Register> uses) =>
        this;

    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        throw new NotImplementedException();
    }
}
