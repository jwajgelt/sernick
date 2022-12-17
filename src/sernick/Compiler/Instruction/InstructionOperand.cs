namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Utility;

public interface IInstructionOperand
{
    IEnumerable<Register> RegistersUsed { get; }
}

public sealed record RegInstructionOperand(Register Register) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Register.Enumerate();
}

/// <summary>
/// [<see cref="BaseReg"/> + <see cref="BaseAddress"/> + <see cref="Displacement"/>]
/// All are optional, but at least one must be specified.
/// </summary>
public sealed record MemInstructionOperand(
    Register? BaseReg,
    Label? BaseAddress,
    RegisterValue? Displacement) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => BaseReg.Enumerate().OfType<Register>();
}

public sealed record ImmInstructionOperand(RegisterValue Value) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();
}

public static class InstructionOperandExtensions
{
    public static RegInstructionOperand AsRegOperand(this Register register) => new(register);

    public static MemInstructionOperand AsMemOperand(this Register location) => new(location, null, null);

    public static MemInstructionOperand AsMemOperand(this (Label baseAddress, RegisterValue displacement) location) =>
        new(null, location.baseAddress, location.displacement);

    public static ImmInstructionOperand AsOperand(this RegisterValue value) => new(value);
}
