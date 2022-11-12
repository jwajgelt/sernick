namespace sernick.Ast.Analysis.NameResolution;

using Nodes;

public sealed record PartialNameResolutionResult(IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations,
    IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations,
    IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations)
{
    public PartialNameResolutionResult() : this(new Dictionary<VariableValue, Declaration>(),
        new Dictionary<Assignment, VariableDeclaration>(),
        new Dictionary<FunctionCall, FunctionDefinition>())
    {
    }

    public static PartialNameResolutionResult Join(params PartialNameResolutionResult[] results)
    {
        return new PartialNameResolutionResult(
            MergeDictionaries(results.Select(result => result.UsedVariableDeclarations)),
            MergeDictionaries(results.Select(result => result.AssignedVariableDeclarations)),
            MergeDictionaries(results.Select(result => result.CalledFunctionDeclarations))
        );
    }

    public static PartialNameResolutionResult OfVariableUse(VariableValue node, Declaration declaration)
    {
        return new PartialNameResolutionResult(new Dictionary<VariableValue, Declaration> { { node, declaration } },
            new Dictionary<Assignment, VariableDeclaration>(),
            new Dictionary<FunctionCall, FunctionDefinition>());
    }

    public static PartialNameResolutionResult OfAssignment(Assignment node, VariableDeclaration declaration)
    {
        return new PartialNameResolutionResult(new Dictionary<VariableValue, Declaration>(),
            new Dictionary<Assignment, VariableDeclaration> { { node, declaration } },
            new Dictionary<FunctionCall, FunctionDefinition>());
    }

    public static PartialNameResolutionResult OfFunctionCall(FunctionCall node, FunctionDefinition declaration)
    {
        return new PartialNameResolutionResult(new Dictionary<VariableValue, Declaration>(),
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
