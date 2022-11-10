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

    public static NameResolutionPartialResult Join(IEnumerable<NameResolutionPartialResult> results)
    {
        var resultsArray = results as NameResolutionPartialResult[] ?? results.ToArray();
        return new NameResolutionPartialResult(
            MergeDictionaries(resultsArray.Select(result => result.UsedVariableDeclarations)),
            MergeDictionaries(resultsArray.Select(result => result.AssignedVariableDeclarations)),
            MergeDictionaries(resultsArray.Select(result => result.CalledFunctionDeclarations))

        );
    }

    private static IReadOnlyDictionary<K, V> MergeDictionaries<K, V>(IEnumerable<IReadOnlyDictionary<K, V>> dictionaries) where K : notnull
    {
        return dictionaries.SelectMany(dict => dict)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
