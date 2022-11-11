namespace sernick.Ast.Analysis.NameResolution;

using System.Collections.Immutable;
using Diagnostics;
using Nodes;

public class NameResolutionLocallyVisibleVariables
{
    public ImmutableDictionary<string, Declaration> Variables { get; }
    private readonly ImmutableHashSet<string> _currentScope;
    private readonly IDiagnostics _diagnostics;
    
    private NameResolutionLocallyVisibleVariables(ImmutableDictionary<string, Declaration> variables,
        ImmutableHashSet<string> currentScope, IDiagnostics diagnostics)
    {
        Variables = variables;
        _currentScope = currentScope;
        _diagnostics = diagnostics;
    }
    public NameResolutionLocallyVisibleVariables(IDiagnostics diagnostics) : this(ImmutableDictionary<string, Declaration>.Empty, ImmutableHashSet<string>.Empty, diagnostics)
    {
    }

    public NameResolutionLocallyVisibleVariables Add(VariableDeclaration declaration)
    {
        return Add(declaration.Name.Name, declaration);
    }
    
    
    public NameResolutionLocallyVisibleVariables Add(FunctionParameterDeclaration declaration)
    {
        return Add(declaration.Name.Name, declaration);
    }
    
    
    public NameResolutionLocallyVisibleVariables Add(FunctionDefinition declaration)
    {
        return Add(declaration.Name.Name, declaration);
    }

    public NameResolutionLocallyVisibleVariables NewScope()
    {
        return new NameResolutionLocallyVisibleVariables(Variables, ImmutableHashSet<string>.Empty, _diagnostics);
    }

    private NameResolutionLocallyVisibleVariables Add(string name, Declaration declaration)
    {
        return new NameResolutionLocallyVisibleVariables(Variables.Remove(name).Add(name, declaration), _currentScope.Add(name), _diagnostics);
    }
}
