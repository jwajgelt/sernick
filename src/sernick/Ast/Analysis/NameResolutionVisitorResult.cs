namespace sernick.Ast.Analysis;

public record NameResolutionVisitorResult(NameResolutionPartialResult PartialResult, NameResolutionLocallyVisibleVariables Variables)
{
    public NameResolutionVisitorResult(NameResolutionLocallyVisibleVariables variables) : this(
        new NameResolutionPartialResult(), variables)
    {
    }
}
