namespace sernick.Compiler.Function;

using Ast.Nodes;
using CodeGeneration;

public static class LabelGenerator
{
    private const string FUN_DELIMITER = ".";
    private const string NUM_DELIMITER = "#";

    /// <summary>
    /// Generates label for later use in ASM code.
    /// Assumes that if parent is not null, it already has a label.
    /// </summary>
    public static readonly GenerateLabel Generate = (parent, name, num) =>
    {
        if (parent is null)
        {
            return "Main";
        }

        if (num.HasValue)
        {
            return $"{parent.Label.Value}{FUN_DELIMITER}{name.Name}{NUM_DELIMITER}{num.Value}";
        }

        return $"{parent.Label.Value}{FUN_DELIMITER}{name.Name}";
    };

    public delegate Label GenerateLabel(IFunctionContext? parent, Identifier name, int? distinctionNumber);
}
