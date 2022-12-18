namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Utility;

public interface IInstructionOperand : IAsmable
{
    IEnumerable<Register> RegistersUsed { get; }
}

public sealed record RegInstructionOperand(Register Register) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Register.Enumerate();
    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        var reg = registerMapping[Register];
        return $"{reg.ToString().ToLower()}";
    }
}

/// <summary>
/// <see cref="BaseAddress"/>[<see cref="BaseReg"/> <see cref="Displacement"/>]
/// </summary>
public sealed record MemInstructionOperand(
    Label? BaseAddress,
    Register? BaseReg,
    RegisterValue? Displacement) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => BaseReg.Enumerate().OfType<Register>();
    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        var baseSegment = BaseAddress is not null ? $"{BaseAddress.Value}" : "";
        var regSegment = BaseReg is not null ? $"{registerMapping[BaseReg].ToString().ToLower()}" : "";
        var displacementSegment = Displacement is not null ? $"+ {Displacement.Value}" : "";
        return $"{baseSegment}[{regSegment}{displacementSegment}]";
    }
}

public sealed record ImmInstructionOperand(RegisterValue Value) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public override string ToString() => Value.ToString();
    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return $"{Value.Value}";
    }
}

public static class InstructionOperandExtensions
{
    public static RegInstructionOperand AsRegOperand(this Register register) => new(register);

    public static MemInstructionOperand AsMemOperand(this Register location) => new(null, location, null);

    public static MemInstructionOperand AsMemOperand(this (Label baseAddress, RegisterValue displacement) location) =>
        new(location.baseAddress, null, location.displacement);

    public static ImmInstructionOperand AsOperand(this RegisterValue value) => new(value);
}
