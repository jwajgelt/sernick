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
        var newDict = Variables.Add(declaration.Name.Name, declaration);
        return new NameResolutionLocallyVisibleVariables(newDict);
    }
    
    
    public NameResolutionLocallyVisibleVariables Add(FunctionParameterDeclaration declaration)
    {
        var newDict = Variables.Add(declaration.Name.Name, declaration);
        return new NameResolutionLocallyVisibleVariables(newDict);
    }
    
    
    public NameResolutionLocallyVisibleVariables Add(FunctionDefinition declaration)
    {
        var newDict = Variables.Add(declaration.Name.Name, declaration);
        return new NameResolutionLocallyVisibleVariables(newDict);
    }
}
