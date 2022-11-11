namespace sernick.Ast.Analysis.NameResolution;

using System.Collections.Immutable;
using Diagnostics;
using Nodes;

public class LocalVariablesManager
{
    public ImmutableDictionary<string, Declaration> Variables { get; }
    private readonly ImmutableHashSet<string> _currentScope;
    private readonly IDiagnostics _diagnostics;
    
    private LocalVariablesManager(ImmutableDictionary<string, Declaration> variables,
        ImmutableHashSet<string> currentScope, IDiagnostics diagnostics)
    {
        Variables = variables;
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
        return new LocalVariablesManager(Variables, ImmutableHashSet<string>.Empty, _diagnostics);
    }

    private LocalVariablesManager Add(string name, Declaration declaration)
    {
        return new LocalVariablesManager(Variables.Remove(name).Add(name, declaration), _currentScope.Add(name), _diagnostics);
    }
}
