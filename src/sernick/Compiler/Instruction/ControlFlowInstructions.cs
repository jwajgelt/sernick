namespace sernick.Compiler.Instruction;

using CodeGeneration;

public record struct Label(string Name)
{
    public static implicit operator Label(string name) => new(name);
}

public sealed record CallInstruction(Label Location) : IInstruction;
