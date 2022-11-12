namespace sernick.Ast.Analysis.NameResolution;

using Nodes;
using Utility;

public sealed record PartialNameResolutionResult(IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations,
    IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations,
    IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations)
{
    public PartialNameResolutionResult() : this(new Dictionary<VariableValue, Declaration>(),
        new Dictionary<Assignment, VariableDeclaration>(),
        new Dictionary<FunctionCall, FunctionDefinition>())
    {
    }

    public PartialNameResolutionResult JoinWith(PartialNameResolutionResult other)
    {
        return new PartialNameResolutionResult(
            UsedVariableDeclarations.JoinWith(other.UsedVariableDeclarations),
            AssignedVariableDeclarations.JoinWith(other.AssignedVariableDeclarations),
            CalledFunctionDeclarations.JoinWith(other.CalledFunctionDeclarations)
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
}
