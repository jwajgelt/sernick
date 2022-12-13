namespace sernick.CodeGeneration;

using ControlFlowGraph.CodeTree;

public interface IAsmable
{
    string ToAsm();
}

public interface IInstruction : IAsmable
{
    IEnumerable<Register> RegistersDefined { get; }
    IEnumerable<Register> RegistersUsed { get; }
    bool PossibleFollow { get; }
    Label? PossibleJump { get; }
    bool IsCopy { get; }
}

public sealed record Label(string Value) : IAsmable
{
    public static implicit operator Label(string name) => new(name);
    public string ToAsm()
    {
        throw new NotImplementedException();
    }
}
