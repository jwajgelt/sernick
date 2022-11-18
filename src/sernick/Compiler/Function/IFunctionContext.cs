namespace sernick.Compiler.Function;

public abstract record FunctionVariable;

public interface IFunctionContext : IFunctionCaller
{
    public void AddLocal(FunctionVariable variable, bool usedElsewhere);
}
