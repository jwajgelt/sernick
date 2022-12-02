namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

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

public abstract record ConditionalInstruction(ConditionCode Code) : IInstruction;

public sealed record SetCcInstruction(ConditionCode Code, Register Register) : ConditionalInstruction(Code);
