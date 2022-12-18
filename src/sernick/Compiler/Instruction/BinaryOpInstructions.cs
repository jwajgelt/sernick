namespace sernick.Compiler.Instruction;

using Ast.Nodes;
using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Utility;

/// <summary>
/// Create binary-op instructions using static factory methods and the builder.
/// <example>
/// <code>
/// using Bin = BinaryOpInstruction;
/// Bin.Add.ToReg(reg).FromImm(5) // add $reg, 5
/// </code>
/// </example>
/// </summary>
public abstract record BinaryOpInstruction(IInstructionOperand Left, IInstructionOperand Right) : IInstruction
{
    public static BinaryAssignInstruction.Builder Add => new(BinaryAssignInstructionOp.Add);
    public static BinaryAssignInstruction.Builder Sub => new(BinaryAssignInstructionOp.Sub);
    public static BinaryAssignInstruction.Builder And => new(BinaryAssignInstructionOp.And);
    public static BinaryAssignInstruction.Builder Or => new(BinaryAssignInstructionOp.Or);
    public static BinaryAssignInstruction.Builder Xor => new(BinaryAssignInstructionOp.Xor);
    public static BinaryComputeInstruction.Builder Cmp => new(BinaryComputeInstructionOp.Cmp);

    public abstract IEnumerable<Register> RegistersDefined { get; }

    public IEnumerable<Register> RegistersUsed =>
        Left.Enumerate().Append(Right)
            .SelectMany(operand => operand.RegistersUsed);

    public bool PossibleFollow => true;

    public Label? PossibleJump => null;

    public bool IsCopy => false;
    public abstract string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping);
}

public enum BinaryAssignInstructionOp
{
    Add, Sub,
    And, Or, Xor,
}

public record BinaryAssignInstruction(BinaryAssignInstructionOp Op, IInstructionOperand Left, IInstructionOperand Right) : BinaryOpInstruction(Left, Right)
{
    public sealed record Builder(BinaryAssignInstructionOp Op)
    {
        private IInstructionOperand? _target;

        public Builder ToReg(Register target)
        {
            _target = target.AsRegOperand();
            return this;
        }

        public Builder ToMem(Register location)
        {
            _target = location.AsMemOperand();
            return this;
        }

        public BinaryAssignInstruction FromReg(Register source) => new(Op, _target!, source.AsRegOperand());
        public BinaryAssignInstruction FromMem(Register location) => new(Op, _target!, location.AsMemOperand());
        public BinaryAssignInstruction FromImm(RegisterValue value) => new(Op, _target!, value.AsOperand());
    }

    public override IEnumerable<Register> RegistersDefined =>
        Left.Enumerate().OfType<RegInstructionOperand>().Select(operand => operand.Register);

    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        var leftSegment = Left.ToAsm(registerMapping);
        var rightSegment = Right.ToAsm(registerMapping);
        return $"\t{Op.ToString().ToLower()}\t{leftSegment}, {rightSegment}";
    }
}

public enum BinaryComputeInstructionOp
{
    Cmp
}

public sealed record BinaryComputeInstruction(BinaryComputeInstructionOp Op, IInstructionOperand Left, IInstructionOperand Right) : BinaryOpInstruction(Left, Right)
{
    public sealed record Builder(BinaryComputeInstructionOp Op)
    {
        private IInstructionOperand? _target;

        public Builder ToReg(Register target)
        {
            _target = target.AsRegOperand();
            return this;
        }

        public Builder ToMem(Register location)
        {
            _target = location.AsMemOperand();
            return this;
        }

        public BinaryComputeInstruction FromReg(Register source) => new(Op, _target!, source.AsRegOperand());
        public BinaryComputeInstruction FromMem(Register location) => new(Op, _target!, location.AsMemOperand());
        public BinaryComputeInstruction FromImm(RegisterValue value) => new(Op, _target!, value.AsOperand());
    }

    public override IEnumerable<Register> RegistersDefined => Enumerable.Empty<Register>();
    public override string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        var leftSegment = Left.ToAsm(registerMapping);
        var rightSegment = Right.ToAsm(registerMapping);
        return $"\t{Op.ToString().ToLower()}\t{leftSegment}, {rightSegment}";
    }
}
