namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Function;

public abstract record TransferControlInstruction(Label Location) : IInstruction
{
    public abstract IEnumerable<Register> RegistersDefined { get; }

    public abstract IEnumerable<Register> RegistersUsed { get; }

    public abstract bool PossibleFollow { get; }

    public abstract Label? PossibleJump { get; }

    public bool IsCopy => false;

    public IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map) => this;

    public abstract string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping);
}

public sealed record CallInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override IEnumerable<Register> RegistersDefined => Convention.CallerToSave;

    public override IEnumerable<Register> RegistersUsed => Convention.ArgumentRegisters;

    public override bool PossibleFollow => true;

    public override Label? PossibleJump => null;

    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return $"\tcall\t{Location.Value}";
    }
}

public sealed record JmpInstruction(Label Location) : TransferControlInstruction(Location)
{
    public override IEnumerable<Register> RegistersDefined => Enumerable.Empty<Register>();

    public override IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public override bool PossibleFollow => false;

    public override Label PossibleJump => Location;

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
