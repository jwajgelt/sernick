namespace sernick.Ast.Analysis.NameResolution;

using Nodes;
using Utility;

/// <summary>
///     Holds results of name resolution on AST.
/// </summary>
/// <param name="UsedVariableDeclarations">
///     Maps uses of variables to the declarations of these variables
/// </param>
/// <param name="AssignedVariableDeclarations">
///     Maps left-hand sides of assignments to variables
///     to the declarations of these variables.
///     NOTE: Since function parameters are non-assignable,
///     these can only be variable declarations (`var x`, `const y`)
/// </param>
/// <param name="CalledFunctionDeclarations">
///     Maps AST nodes for function calls
///     to that function's declaration
/// </param>
public sealed record NameResolutionResult(IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations,
    IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations,
    IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations)
{
    public NameResolutionResult() : this(new Dictionary<VariableValue, Declaration>(),
        new Dictionary<Assignment, VariableDeclaration>(),
        new Dictionary<FunctionCall, FunctionDefinition>())
    {
    }

    public NameResolutionResult JoinWith(NameResolutionResult other)
    {
        return new NameResolutionResult(
            UsedVariableDeclarations.JoinWith(other.UsedVariableDeclarations),
            AssignedVariableDeclarations.JoinWith(other.AssignedVariableDeclarations),
            CalledFunctionDeclarations.JoinWith(other.CalledFunctionDeclarations)
        );
    }

    public static NameResolutionResult OfVariableUse(VariableValue node, Declaration declaration)
    {
        return new NameResolutionResult(new Dictionary<VariableValue, Declaration> { { node, declaration } },
            new Dictionary<Assignment, VariableDeclaration>(),
            new Dictionary<FunctionCall, FunctionDefinition>());
    }

    public static NameResolutionResult OfAssignment(Assignment node, VariableDeclaration declaration)
    {
        return new NameResolutionResult(new Dictionary<VariableValue, Declaration>(),
            new Dictionary<Assignment, VariableDeclaration> { { node, declaration } },
            new Dictionary<FunctionCall, FunctionDefinition>());
    }

    public static NameResolutionResult OfFunctionCall(FunctionCall node, FunctionDefinition declaration)
    {
        return new NameResolutionResult(new Dictionary<VariableValue, Declaration>(),
            new Dictionary<Assignment, VariableDeclaration>(),
            new Dictionary<FunctionCall, FunctionDefinition> { { node, declaration } });
    }
}