namespace sernick.Ast.Analysis.NameResolution;

using Nodes;

public sealed record PartialAlgorithmResult(IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations,
    IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations,
    IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations)
{
    public PartialAlgorithmResult() : this(new Dictionary<VariableValue, Declaration>(),
        new Dictionary<Assignment, VariableDeclaration>(),
        new Dictionary<FunctionCall, FunctionDefinition>())
    {
    }

    public static PartialAlgorithmResult Join(params PartialAlgorithmResult[] results)
    {
        return new PartialAlgorithmResult(
            MergeDictionaries(results.Select(result => result.UsedVariableDeclarations)),
            MergeDictionaries(results.Select(result => result.AssignedVariableDeclarations)),
            MergeDictionaries(results.Select(result => result.CalledFunctionDeclarations))
        );
    }

    public static PartialAlgorithmResult OfVariableUse(VariableValue node, Declaration declaration)
    {
        return new PartialAlgorithmResult(new Dictionary<VariableValue, Declaration> { { node, declaration } },
            new Dictionary<Assignment, VariableDeclaration>(),
            new Dictionary<FunctionCall, FunctionDefinition>());
    }

    public static PartialAlgorithmResult OfAssignment(Assignment node, VariableDeclaration declaration)
    {
        return new PartialAlgorithmResult(new Dictionary<VariableValue, Declaration>(),
            new Dictionary<Assignment, VariableDeclaration> { { node, declaration } },
            new Dictionary<FunctionCall, FunctionDefinition>());
    }

    public static PartialAlgorithmResult OfFunctionCall(FunctionCall node, FunctionDefinition declaration)
    {
        return new PartialAlgorithmResult(new Dictionary<VariableValue, Declaration>(),
            new Dictionary<Assignment, VariableDeclaration>(),
            new Dictionary<FunctionCall, FunctionDefinition> { { node, declaration } });
    }

    private static IReadOnlyDictionary<K, V> MergeDictionaries<K, V>(
        IEnumerable<IReadOnlyDictionary<K, V>> dictionaries) where K : notnull
    {
        return dictionaries.SelectMany(dict => dict)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
