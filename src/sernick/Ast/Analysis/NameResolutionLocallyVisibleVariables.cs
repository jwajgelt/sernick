namespace sernick.Ast.Analysis;

using System.Collections.Immutable;
using Nodes;

public record NameResolutionLocallyVisibleVariables(ImmutableDictionary<string, Declaration> Variables)
{
    public NameResolutionLocallyVisibleVariables() : this(ImmutableDictionary<string, Declaration>.Empty)
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

    private NameResolutionLocallyVisibleVariables Add(string name, Declaration declaration)
    {
        return new NameResolutionLocallyVisibleVariables(Variables.Remove(name).Add(name, declaration));
    }
}
