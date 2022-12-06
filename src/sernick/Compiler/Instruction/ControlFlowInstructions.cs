namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public record struct Label(string Name)
{
    public static implicit operator Label(string name) => new(name);
}

public sealed record CallInstruction(Label Location) : IInstruction
{
    public IEnumerable<Register> RegistersDefined()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Register> RegistersUsed()
    {
        throw new NotImplementedException();
    }

    public bool PossibleFollow => throw new NotImplementedException();
    public CodeGeneration.Label? PossibleJump => throw new NotImplementedException();
    public bool IsCopy => throw new NotImplementedException();
}
