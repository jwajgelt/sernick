namespace sernick.Ast.Analysis.NameResolution;

public record NameResolutionVisitorResult(PartialNameResolutionResult PartialResult, IdentifiersNamespace IdentifiersNamespace)
{
    public NameResolutionVisitorResult(IdentifiersNamespace identifiersNamespace) : this(
        new PartialNameResolutionResult(), identifiersNamespace)
    {
    }
}
