namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Utility;

public interface IInstructionOperand : IAsmable
{
    IEnumerable<Register> RegistersUsed { get; }
    IInstructionOperand MapRegisters(IReadOnlyDictionary<Register, Register> map);
}

public sealed record RegInstructionOperand(Register Register) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Register.Enumerate();
    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        var reg = registerMapping[Register];
        return $"{reg.ToString().ToLower()}";
    }

    public IInstructionOperand MapRegisters(IReadOnlyDictionary<Register, Register> map) =>
        new RegInstructionOperand(map.GetOrKey(Register));
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
    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        var regSegment = BaseReg is not null ? registerMapping[BaseReg].ToString().ToLower() : null;
        var baseSegment = BaseAddress?.Value;
        var displacementSegment = Displacement?.Value.ToString();
        var intermediateResult = $"[{string.Join(" + ", new[] { regSegment, baseSegment, displacementSegment }.OfType<string>())}]";
        return intermediateResult.Replace("+ -", "- ");
    }

    public IInstructionOperand MapRegisters(IReadOnlyDictionary<Register, Register> map) =>
        this with { BaseReg = map.GetOrKey(BaseReg) };
}

public sealed record ImmInstructionOperand(RegisterValue Value) : IInstructionOperand
{
    public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();

    public override string ToString() => Value.ToString();
    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return $"{Value.Value}";
    }
    public IInstructionOperand MapRegisters(IReadOnlyDictionary<Register, Register> map) => this;
}

public static class InstructionOperandExtensions
{
    public static RegInstructionOperand AsRegOperand(this Register register) => new(register);

    public static MemInstructionOperand AsMemOperand(this Register location) => new(location, null, null);

    public static MemInstructionOperand AsMemOperand(this (Label baseAddress, RegisterValue displacement) location) =>
        new(null, location.baseAddress, location.displacement);

    public static ImmInstructionOperand AsOperand(this RegisterValue value) => new(value);
}
