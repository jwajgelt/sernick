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
}

public sealed record SetCcInstruction(ConditionCode Code, Register Register) : ConditionalInstruction(Code)
{
    public override IEnumerable<Register> RegistersDefined => Register.Enumerate();

    public override IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public override bool PossibleFollow => true;

    public override Label? PossibleJump => null;

    public override bool IsCopy => false;
}
