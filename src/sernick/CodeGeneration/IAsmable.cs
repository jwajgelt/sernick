namespace sernick.CodeGeneration;

using ControlFlowGraph.CodeTree;

public interface IAsmable
{
    string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping);
}

public interface IInstruction : IAsmable
{
    IEnumerable<Register> RegistersDefined { get; }
    IEnumerable<Register> RegistersUsed { get; }
    bool PossibleFollow { get; }
    Label? PossibleJump { get; }
    bool IsCopy { get; }
    IInstruction ReplaceRegisters(Dictionary<Register, Register> defines, Dictionary<Register, Register> uses);
}

public sealed record Label(string Value) : IAsmable
{
    public static implicit operator Label(string name) => new(name);
    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        throw new NotImplementedException();
    }
}
