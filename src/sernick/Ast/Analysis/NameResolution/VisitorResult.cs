namespace sernick.Ast.Analysis.NameResolution;

public record VisitorResult(PartialAlgorithmResult PartialAlgorithmResult, IdentifiersNamespace IdentifiersNamespace)
{
    public VisitorResult(IdentifiersNamespace identifiersNamespace) : this(
        new PartialAlgorithmResult(), identifiersNamespace)
    {
    }
}
