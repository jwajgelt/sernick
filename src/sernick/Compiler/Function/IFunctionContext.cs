namespace sernick.Compiler.Function;

public abstract record FunctionVariable;

public interface IFunctionContext : IFunctionCaller
{
    public abstract void AddLocal(FunctionVariable variable, bool usedElsewhere);
}
