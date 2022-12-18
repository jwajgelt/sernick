namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Utility;

public enum UnaryOp
{
    Not, Neg
}

/// <summary>
/// Create unary-op instructions using static factory methods and the builder.
/// <example>
/// <code>
/// using Un = UnaryOpInstruction;
/// Un.Not.Reg(reg) // not $reg
/// </code>
/// </example>
/// </summary>
public sealed record UnaryOpInstruction(UnaryOp Op, IInstructionOperand Operand) : IInstruction
{
    public static UnaryOpInstructionBuilder Not => new(UnaryOp.Not);
    public static UnaryOpInstructionBuilder Neg => new(UnaryOp.Neg);

    public sealed record UnaryOpInstructionBuilder(UnaryOp Op)
    {
        public UnaryOpInstruction Reg(Register target) => new(Op, target.AsRegOperand());
        public UnaryOpInstruction Mem(Register location) => new(Op, location.AsMemOperand());
    }

    public IEnumerable<Register> RegistersDefined =>
        Operand.Enumerate().OfType<RegInstructionOperand>().Select(operand => operand.Register);

    public IEnumerable<Register> RegistersUsed => Operand.RegistersUsed;

    public bool PossibleFollow => true;

    public Label? PossibleJump => null;

    public bool IsCopy => false;

    public IInstruction ReplaceRegisters(Dictionary<Register, Register> defines, Dictionary<Register, Register> uses) =>
        Operand switch
        {
            RegInstructionOperand => new UnaryOpInstruction(Op, Operand.ReplaceRegisters(defines)),
            _ => new UnaryOpInstruction(Op, Operand.ReplaceRegisters(uses)),
        };

    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        throw new NotImplementedException();
    }
}
