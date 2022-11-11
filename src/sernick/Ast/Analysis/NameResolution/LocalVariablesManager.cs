namespace sernick.Ast.Analysis.NameResolution;

using System.Collections.Immutable;
using Diagnostics;
using Errors;
using Nodes;

public class LocalVariablesManager
{
    private readonly ImmutableDictionary<string, Declaration> _variables;
    private readonly ImmutableHashSet<string> _currentScope;
    private readonly IDiagnostics _diagnostics;

    private LocalVariablesManager(ImmutableDictionary<string, Declaration> variables,
        ImmutableHashSet<string> currentScope, IDiagnostics diagnostics)
    {
        _variables = variables;
        _currentScope = currentScope;
        _diagnostics = diagnostics;
    }
    public LocalVariablesManager(IDiagnostics diagnostics) : this(ImmutableDictionary<string, Declaration>.Empty, ImmutableHashSet<string>.Empty, diagnostics)
    {
    }

    public LocalVariablesManager Add(VariableDeclaration declaration)
    {
        return Add(declaration.Name.Name, declaration);
    }

    public LocalVariablesManager Add(FunctionParameterDeclaration declaration)
    {
        return Add(declaration.Name.Name, declaration);
    }

    public LocalVariablesManager Add(FunctionDefinition declaration)
    {
        return Add(declaration.Name.Name, declaration);
    }

    public LocalVariablesManager NewScope()
    {
        return new LocalVariablesManager(_variables, ImmutableHashSet<string>.Empty, _diagnostics);
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

    private LocalVariablesManager Add(string name, Declaration declaration)
    {
        if (_currentScope.Contains(name))
        {
            _diagnostics.Report(new MultipleDeclarationsOfTheSameIdentifierError(_variables[name], declaration));
        }

        return new LocalVariablesManager(_variables.Remove(name).Add(name, declaration), _currentScope.Add(name), _diagnostics);
    }

    private Declaration? GetDeclaration(Identifier identifier)
    {
        var name = identifier.Name;
        if (_variables.ContainsKey(name))
        {
            return _variables[name];
        }

        _diagnostics.Report(new UndeclaredIdentifierError(identifier));
        return null;
    }
}
