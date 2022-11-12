namespace sernick.Ast.Analysis.NameResolution;

public record struct NameResolutionVisitorResult(PartialNameResolutionResult PartialResult, IdentifiersNamespace IdentifiersNamespace)
{
    public NameResolutionVisitorResult(IdentifiersNamespace identifiersNamespace) : this(
        new PartialNameResolutionResult(), identifiersNamespace)
    {
    }
}
