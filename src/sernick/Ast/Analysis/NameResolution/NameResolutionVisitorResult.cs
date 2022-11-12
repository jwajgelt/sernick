namespace sernick.Ast.Analysis.NameResolution;

public record struct NameResolutionVisitorResult(NameResolutionResult Result, IdentifiersNamespace2 IdentifiersNamespace)
{
    public NameResolutionVisitorResult(IdentifiersNamespace2 identifiersNamespace) : this(
        new NameResolutionResult(), identifiersNamespace)
    {
    }
}
