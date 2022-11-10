namespace sernick.Ast.Analysis;


public record NameResolutionVisitorParams(NameResolutionLocallyVisibleVariables Variables)
{
    public NameResolutionVisitorParams() : this(new NameResolutionLocallyVisibleVariables())
    {
    }
}
