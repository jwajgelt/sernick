namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public sealed record CallInstruction(Label Location) : IInstruction
{
    public IEnumerable<Register> RegistersDefined => throw new NotImplementedException();
    public IEnumerable<Register> RegistersUsed => throw new NotImplementedException();
    public bool PossibleFollow => throw new NotImplementedException();
    public CodeGeneration.Label? PossibleJump => throw new NotImplementedException();
    public bool IsCopy => throw new NotImplementedException();
}
