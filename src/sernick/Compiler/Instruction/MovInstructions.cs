namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Utility;

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

    public static MovInstructionBuilder ToMem(Label baseAddress, RegisterValue displacement) =>
        new((baseAddress, displacement).AsMemOperand());

    public sealed record MovInstructionBuilder(IInstructionOperand Target)
    {
        public MovInstruction FromReg(Register source) => new(Target, source.AsRegOperand());
        public MovInstruction FromMem(Register location) => new(Target, location.AsMemOperand());
        public MovInstruction FromMem(Label baseAddress, RegisterValue displacement) =>
            new(Target, (baseAddress, displacement).AsMemOperand());
        public MovInstruction FromImm(RegisterValue value) => new(Target, value.AsOperand());
    }

    public IEnumerable<Register> RegistersDefined =>
        Target.Enumerate().OfType<RegInstructionOperand>().Select(operand => operand.Register);

    public IEnumerable<Register> RegistersUsed =>
        Target.Enumerate().OfType<MemInstructionOperand>().Append(Source)
            .SelectMany(operand => operand.RegistersUsed);

    public bool PossibleFollow => true;

    public Label? PossibleJump => null;

    public bool IsCopy => Target is RegInstructionOperand && Source is RegInstructionOperand;

    public IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map) =>
        new MovInstruction(Target.MapRegisters(map), Source.MapRegisters(map));

    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        throw new NotImplementedException();
    }
}
