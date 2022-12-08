namespace sernick.Compiler.Function;

using Ast.Nodes;
using CodeGeneration;

public static class LabelGenerator
{
    // We disallow "_" in function names, and it is the only special character allowed in asm labels
    private const string DELIMITER = "_";

    /// <summary>
    /// Generates label for later use in ASM code.
    /// Assumes that if parent is not null, it already has a label.
    /// </summary>
    public static readonly GenerateLabel Generate = (parent, name) =>
         parent is null ? "Main" : $"{parent.Label.Value}{DELIMITER}{name.Name}";

    public delegate Label GenerateLabel(IFunctionContext? parent, Identifier name);
}
