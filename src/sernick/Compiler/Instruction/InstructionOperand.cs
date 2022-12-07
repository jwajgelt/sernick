namespace sernick.Compiler.Instruction;

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

public sealed record MemInstructionOperand(Register Location) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Location.Enumerate();
}

public sealed record ImmInstructionOperand(RegisterValue Value) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();
}

public static class InstructionOperandExtensions
{
    public static RegInstructionOperand AsRegOperand(this Register register) => new(register);

    public static MemInstructionOperand AsMemOperand(this Register location) => new(location);

    public static ImmInstructionOperand AsOperand(this RegisterValue value) => new(value);
}
