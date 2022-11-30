namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public enum ConditionCode
{
    O, No,
    B, Nb,
    E, Ne,
    A, Na,
    S, Ns,
    P, Np,
    L, Nl,
    G, Ng
}

public abstract record ConditionalInstruction(ConditionCode Code) : IInstruction;

public sealed record SetCcInstruction(ConditionCode Code, Register Register) : ConditionalInstruction(Code);
