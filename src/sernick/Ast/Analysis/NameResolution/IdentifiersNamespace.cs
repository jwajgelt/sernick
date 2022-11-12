namespace sernick.Ast.Analysis.NameResolution;

using System.Collections.Immutable;
using Diagnostics;
using Nodes;

/// <summary>
///     Class responsible for managing identifiers visible from a certain place in code and reporting diagnostics.
/// </summary>
public sealed class IdentifiersNamespace
{
    private readonly ImmutableHashSet<string> _currentScope;
    private readonly IDiagnostics _diagnostics;
    private readonly ImmutableDictionary<string, Declaration> _variables;

    public IdentifiersNamespace(IDiagnostics diagnostics) : this(ImmutableDictionary<string, Declaration>.Empty,
        ImmutableHashSet<string>.Empty, diagnostics)
    {
    }

    private IdentifiersNamespace(ImmutableDictionary<string, Declaration> variables,
        ImmutableHashSet<string> currentScope, IDiagnostics diagnostics) =>
        (_variables, _currentScope, _diagnostics) = (variables, currentScope, diagnostics);

    public IdentifiersNamespace Add(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (_currentScope.Contains(name))
        {
            _diagnostics.Report(new MultipleDeclarationsError(_variables[name], declaration));
        }

        return new IdentifiersNamespace(_variables.Remove(name).Add(name, declaration), _currentScope.Add(name),
            _diagnostics);
    }

    public Declaration? GetUsedVariableDeclaration(Identifier identifier)
    {
        var declaration = GetDeclaration(identifier);
        if (declaration is null or VariableDeclaration or FunctionParameterDeclaration)
        {
            return declaration;
        }

        _diagnostics.Report(new NotAVariableError(identifier));
        return null;
    }

    public VariableDeclaration? GetAssignedVariableDeclaration(Identifier identifier)
    {
        var declaration = GetDeclaration(identifier);
        if (declaration is null or VariableDeclaration)
        {
            return (VariableDeclaration?)declaration;
        }

        _diagnostics.Report(new NotAVariableError(identifier));
        return null;
    }

    public FunctionDefinition? GetCalledFunctionDeclaration(Identifier identifier)
    {
        var declaration = GetDeclaration(identifier);
        if (declaration is null or FunctionDefinition)
        {
            return (FunctionDefinition?)declaration;
        }

        _diagnostics.Report(new NotAFunctionError(identifier));
        return null;
    }

    public Declaration GetDeclaration(Identifier identifier)
    {
        return null;
    }

    public IdentifiersNamespace NewScope()
    {
        return new IdentifiersNamespace(_variables, ImmutableHashSet<string>.Empty, _diagnostics);
    }

    // private Declaration? GetDeclaration(Identifier identifier)
    // {
    //     var name = identifier.Name;
    //     if (_variables.TryGetValue(name, out var declaration))
    //     {
    //         return declaration;
    //     }
    //
    //     _diagnostics.Report(new UndeclaredIdentifierError(identifier));
    //     return null;
    // }
}
