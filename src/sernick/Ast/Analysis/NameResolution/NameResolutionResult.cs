namespace sernick.Ast.Analysis.NameResolution;

using System.Diagnostics;
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
/// <param name="StructDeclarations">
///     Maps Struct Identifier to its StructDeclaration.
/// </param>
public sealed record NameResolutionResult(IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations,
    IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations,
    IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations,
    IReadOnlyDictionary<Identifier, StructDeclaration> StructDeclarations)
{
    public NameResolutionResult() : this(new Dictionary<VariableValue, Declaration>(ReferenceEqualityComparer.Instance),
        new Dictionary<Assignment, VariableDeclaration>(ReferenceEqualityComparer.Instance),
        new Dictionary<FunctionCall, FunctionDefinition>(ReferenceEqualityComparer.Instance),
        new Dictionary<Identifier, StructDeclaration>(ReferenceEqualityComparer.Instance))
    {
    }

    public NameResolutionResult JoinWith(NameResolutionResult other)
    {
        return new NameResolutionResult(
            UsedVariableDeclarations.JoinWith(other.UsedVariableDeclarations, ReferenceEqualityComparer.Instance),
            AssignedVariableDeclarations.JoinWith(other.AssignedVariableDeclarations, ReferenceEqualityComparer.Instance),
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

    public static NameResolutionResult OfAssignment(Assignment node, VariableDeclaration declaration)
    {
        return new NameResolutionResult
        {
            AssignedVariableDeclarations = new Dictionary<Assignment, VariableDeclaration> { { node, declaration } }
        };
    }

    public static NameResolutionResult OfFunctionCall(FunctionCall node, FunctionDefinition declaration)
    {
        return new NameResolutionResult
        {
            CalledFunctionDeclarations = new Dictionary<FunctionCall, FunctionDefinition> { { node, declaration } }
        };
    }

    public static NameResolutionResult OfStructs(Dictionary<Identifier, StructDeclaration> structs)
    {
        return new NameResolutionResult
        {
            StructDeclarations = structs
        };
    }

    public NameResolutionResult AddStructs(Dictionary<Identifier, StructDeclaration> structs)
    {
        Trace.WriteLine(StructDeclarations.Count + " " + structs.Count);
        return this with { StructDeclarations = StructDeclarations.JoinWith(structs, ReferenceEqualityComparer.Instance) };
    }
}
