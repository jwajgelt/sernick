namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public enum BinaryOp
{
    Add, Sub,
    And, Or, Xor,
    Cmp
}

/// <summary>
/// Create binary-op instructions using static factory methods and the builder.
/// <example>
/// <code>
/// using Bin = BinaryOpInstruction;
/// Bin.Add.ToReg(reg).FromImm(5) // add $reg, 5
/// </code>
/// </example>
/// </summary>
public sealed record BinaryOpInstruction(BinaryOp Op, IInstructionOperand Left, IInstructionOperand Right) : IInstruction
{
    public static BinaryOpInstructionBuilder Add => new(BinaryOp.Add);
    public static BinaryOpInstructionBuilder Sub => new(BinaryOp.Sub);
    public static BinaryOpInstructionBuilder And => new(BinaryOp.And);
    public static BinaryOpInstructionBuilder Or => new(BinaryOp.Or);
    public static BinaryOpInstructionBuilder Xor => new(BinaryOp.Xor);
    public static BinaryOpInstructionBuilder Cmp => new(BinaryOp.Cmp);

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

    public IEnumerable<Register> RegistersDefined => throw new NotImplementedException();
    public IEnumerable<Register> RegistersUsed => throw new NotImplementedException();
    public bool PossibleFollow => throw new NotImplementedException();
    public CodeGeneration.Label? PossibleJump => throw new NotImplementedException();
    public bool IsCopy => throw new NotImplementedException();
}

