namespace sernick.Ast.Analysis.NameResolution;

using Nodes;
using Utility;

/// <summary>
///     Holds results of name resolution on AST.
/// </summary>
/// <param name="UsedVariableDeclarations">
///     Maps uses of variables to the declarations of these variables
/// </param>
/// <param name="CalledFunctionDeclarations">
///     Maps AST nodes for function calls
///     to that function's declaration
/// </param>
/// <param name="StructDeclarations">
///     Maps Struct Identifier to its StructDeclaration.
/// </param>
public sealed record NameResolutionResult(IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations,
    IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations,
    IReadOnlyDictionary<Identifier, StructDeclaration> StructDeclarations)
{
    public NameResolutionResult() : this(new Dictionary<VariableValue, Declaration>(ReferenceEqualityComparer.Instance),
        new Dictionary<FunctionCall, FunctionDefinition>(ReferenceEqualityComparer.Instance),
        new Dictionary<Identifier, StructDeclaration>(ReferenceEqualityComparer.Instance))
    {
    }

    public NameResolutionResult JoinWith(NameResolutionResult other)
    {
        return new NameResolutionResult(
            UsedVariableDeclarations.JoinWith(other.UsedVariableDeclarations, ReferenceEqualityComparer.Instance),
            CalledFunctionDeclarations.JoinWith(other.CalledFunctionDeclarations, ReferenceEqualityComparer.Instance),
            StructDeclarations.JoinWith(other.StructDeclarations, ReferenceEqualityComparer.Instance)
        );
    }

    public static NameResolutionResult OfVariableUse(VariableValue node, Declaration variableDeclaration)
    {
        return new NameResolutionResult
        {
            UsedVariableDeclarations = new Dictionary<VariableValue, Declaration> { { node, variableDeclaration } }
        };
    }

    public static NameResolutionResult OfFunctionCall(FunctionCall node, FunctionDefinition declaration)
    {
        return new NameResolutionResult
        {
            CalledFunctionDeclarations = new Dictionary<FunctionCall, FunctionDefinition> { { node, declaration } }
        };
    }

    public static NameResolutionResult OfStructs(IReadOnlyDictionary<Identifier, StructDeclaration> structs)
    {
        return new NameResolutionResult
        {
            StructDeclarations = structs
        };
    }

    public NameResolutionResult AddStructs(IReadOnlyDictionary<Identifier, StructDeclaration> structs)
    {
        return this with { StructDeclarations = StructDeclarations.JoinWith(structs, ReferenceEqualityComparer.Instance) };
    }
}
