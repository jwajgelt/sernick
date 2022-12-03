namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

/// <summary>
/// Create mov instructions using the builder.
/// <example>
/// <code>
/// using Mov = MovInstruction;
/// Mov.ToReg(reg).FromImm(5) // mov $reg, 5
/// </code>
/// </example>
/// </summary>
public sealed record MovInstruction(IInstructionOperand Target, IInstructionOperand Source) : IInstruction
{
    public static MovInstructionBuilder ToReg(Register target) => new(target.AsRegOperand());

    public static MovInstructionBuilder ToMem(Register location) => new(location.AsMemOperand());

    public sealed record MovInstructionBuilder(IInstructionOperand Target)
    {
        public MovInstruction FromReg(Register source) => new(Target, source.AsRegOperand());
        public MovInstruction FromMem(Register location) => new(Target, location.AsMemOperand());
        public MovInstruction FromImm(RegisterValue value) => new(Target, value.AsOperand());
    }
}
