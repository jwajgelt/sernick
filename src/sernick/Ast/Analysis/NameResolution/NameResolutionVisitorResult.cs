namespace sernick.Ast.Analysis.NameResolution;

public record struct NameResolutionVisitorResult(NameResolutionResult Result, IdentifiersNamespace IdentifiersNamespace)
{
    public NameResolutionVisitorResult(IdentifiersNamespace identifiersNamespace) : this(
        new NameResolutionResult(), identifiersNamespace)
    {
    }
}
