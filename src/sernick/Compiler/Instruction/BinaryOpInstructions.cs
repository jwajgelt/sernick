namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public enum BinaryOp
{
    Add, Sub, Xor
}

public sealed record BinaryOpInstruction(BinaryOp Op, IInstructionOperand Left, IInstructionOperand Right) : IInstruction
{
    public static BinaryOpInstructionBuilder Add => new(BinaryOp.Add);
    public static BinaryOpInstructionBuilder Sub => new(BinaryOp.Sub);
    public static BinaryOpInstructionBuilder Xor => new(BinaryOp.Xor);

    public sealed record BinaryOpInstructionBuilder(BinaryOp Op)
    {
        private IInstructionOperand? _target;

        public BinaryOpInstructionBuilder ToReg(Register target)
        {
            _target = target.AsRegOperand();
            return this;
        }

        public BinaryOpInstructionBuilder ToMem(Register location)
        {
            _target = location.AsMemOperand();
            return this;
        }

        public BinaryOpInstruction FromReg(Register source) => new(Op, _target!, source.AsRegOperand());
        public BinaryOpInstruction FromMem(Register location) => new(Op, _target!, location.AsMemOperand());
        public BinaryOpInstruction FromImm(RegisterValue value) => new(Op, _target!, value.AsOperand());
    }
}
