namespace sernick.Compiler.Instruction;

using CodeGeneration.InstructionSelection;

public static class CodeTreePatternValuesHelper
{
    public static TValue Get<TValue>(this IReadOnlyDictionary<CodeTreePattern, object> dictionary, CodeTreePattern key)
    {
        var value = dictionary[key];
        if (value is null)
        {
            throw new KeyNotFoundException();
        }

        return (TValue)value;
    }
}
