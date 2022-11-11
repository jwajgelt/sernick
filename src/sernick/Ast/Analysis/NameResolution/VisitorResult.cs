namespace sernick.Ast.Analysis.NameResolution;

public record VisitorResult(PartialAlgorithmResult PartialAlgorithmResult, LocalVariablesManager variablesManager)
{
    public VisitorResult(LocalVariablesManager variablesManager) : this(
        new PartialAlgorithmResult(), variablesManager)
    {
    }
}
