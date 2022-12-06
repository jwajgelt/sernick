namespace sernick.CodeGeneration;

using ControlFlowGraph.CodeTree;

public interface IAsmable { }

public interface IInstruction : IAsmable
{
    IEnumerable<Register> RegistersDefined();
    IEnumerable<Register> RegistersUsed();
    bool PossibleFollow { get; }
    Label? PossibleJump { get; }
    bool IsCopy { get; }
}

public record Label(string Value) : IAsmable;
