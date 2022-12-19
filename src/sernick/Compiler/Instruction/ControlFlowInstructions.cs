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
    public IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map) => this;

    public abstract string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping);
}

public sealed record CallInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return $"\tcall\t{Location.Value}";
    }
}

public sealed record JmpInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return $"\tjmp\t{Location.Value}";
    }
}

public sealed record RetInstruction : IInstruction
{
    public IEnumerable<Register> RegistersDefined => Enumerable.Empty<Register>();

    public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public bool PossibleFollow => false;

    public Label? PossibleJump => null;

    public bool IsCopy => false;
    public IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map) => this;

    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return "\tret";
    }
}
