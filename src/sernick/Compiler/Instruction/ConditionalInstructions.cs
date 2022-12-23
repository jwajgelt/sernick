namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Utility;

/// <summary>
/// https://www.sandpile.org/x86/cc.htm
/// </summary>
public enum ConditionCode
{
    O, No, // overflow
    B, Nb, // below
    E, Ne, // equals
    A, Na, // above
    S, Ns, // sign
    P, Np, // parity
    L, Nl, // less
    G, Ng  // greater
}

public abstract record ConditionalInstruction(ConditionCode Code) : IInstruction
{
    public abstract IEnumerable<Register> RegistersDefined { get; }
    public abstract IEnumerable<Register> RegistersUsed { get; }
    public abstract bool PossibleFollow { get; }
    public abstract Label? PossibleJump { get; }
    public abstract bool IsCopy { get; }

    public abstract IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map);

    public abstract string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping);
}

public sealed record SetCcInstruction(ConditionCode Code, Register Register) : ConditionalInstruction(Code)
{
    public override IEnumerable<Register> RegistersDefined => Register.Enumerate();

    public override IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public override bool PossibleFollow => true;

    public override Label? PossibleJump => null;

    public override bool IsCopy => false;

    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        var reg = registerMapping[Register];
        return $"\tset{Code.ToString().ToLower()}\t{reg.ToString().ToLower()}";
    }

    public override IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map) =>
        new SetCcInstruction(Code, map.GetOrKey(Register));
}

public sealed record JmpCcInstruction(ConditionCode Code, Label Location) : ConditionalInstruction(Code)
{
    public override IEnumerable<Register> RegistersDefined => Enumerable.Empty<Register>();

    public override IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public override bool PossibleFollow => true;

    public override Label PossibleJump => Location;

    public override bool IsCopy => false;

    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return $"\tj{Code.ToString().ToLower()}\t{Location.Value}";
    }

    public override IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map) => this;
}
