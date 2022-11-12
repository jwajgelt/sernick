namespace sernick.Ast.Analysis.NameResolution;

public record VisitorResult(PartialNameResolutionResult PartialResult, IdentifiersNamespace IdentifiersNamespace)
{
    public VisitorResult(IdentifiersNamespace identifiersNamespace) : this(
        new PartialNameResolutionResult(), identifiersNamespace)
    {
    }
}
