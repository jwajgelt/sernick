namespace sernick.Compiler.Instruction;

using ControlFlowGraph.CodeTree;

public interface IInstructionOperand { }

public sealed record RegInstructionOperand(Register Register) : IInstructionOperand;

public sealed record MemInstructionOperand(Register Location) : IInstructionOperand;

public sealed record ImmInstructionOperand(RegisterValue Value) : IInstructionOperand;

public static class InstructionOperandExtensions
{
    public static RegInstructionOperand AsRegOperand(this Register register) => new(register);

    public static MemInstructionOperand AsMemOperand(this Register location) => new(location);

    public static ImmInstructionOperand AsOperand(this RegisterValue value) => new(value);
}
