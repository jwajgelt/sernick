namespace sernick.Ast.Analysis.NameResolution;

public record VisitorResult(PartialAlgorithmResult PartialAlgorithmResult, NameResolutionLocallyVisibleVariables Variables)
{
    public VisitorResult(NameResolutionLocallyVisibleVariables variables) : this(
        new PartialAlgorithmResult(), variables)
    {
    }
}
