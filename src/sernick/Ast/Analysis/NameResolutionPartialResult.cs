namespace sernick.Ast.Analysis;

using Nodes;

// TODO: replace dicts with immutables

public record NameResolutionPartialResult(IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations,
    IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations,
    IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations)
{
    public NameResolutionPartialResult() : this(new Dictionary<VariableValue, Declaration>(),
        new Dictionary<Assignment, VariableDeclaration>(),
        new Dictionary<FunctionCall, FunctionDefinition>())
    {
    }

    public static NameResolutionPartialResult Join(params NameResolutionPartialResult[] results)
    {
        return new NameResolutionPartialResult(
            MergeDictionaries(results.Select(result => result.UsedVariableDeclarations)),
            MergeDictionaries(results.Select(result => result.AssignedVariableDeclarations)),
            MergeDictionaries(results.Select(result => result.CalledFunctionDeclarations))

        );
    }

    private static IReadOnlyDictionary<K, V> MergeDictionaries<K, V>(IEnumerable<IReadOnlyDictionary<K, V>> dictionaries) where K : notnull
    {
        return dictionaries.SelectMany(dict => dict)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public static NameResolutionPartialResult OfUsedVariable(VariableValue node, Declaration declaration)
    {
        return null;
    }
    public static NameResolutionPartialResult OfAssignment(Assignment node, Declaration declaration)
    {
        return null;
    }
    
    public static NameResolutionPartialResult OfCalledFunction(FunctionCall node, Declaration declaration)
    {
        return null;
    }
}
