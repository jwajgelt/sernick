namespace sernick.Ast.Analysis;

using System.Collections.Immutable;
using Nodes;

public record NameResolutionLocallyVisibleVariables(ImmutableDictionary<string, Declaration> Variables)
{
    public NameResolutionLocallyVisibleVariables() : this(ImmutableDictionary<string, Declaration>.Empty)
    {
    }
}
