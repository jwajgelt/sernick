namespace sernick.Ast.Analysis.NameResolution;

public record VisitorResult(PartialAlgorithmResult PartialAlgorithmResult, LocalVariablesManager VariablesManager)
{
    public VisitorResult(LocalVariablesManager variablesManager) : this(
        new PartialAlgorithmResult(), variablesManager)
    {
    }
}
