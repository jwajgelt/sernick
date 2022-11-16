namespace sernick.Compiler.Function;

public abstract record FunctionVariable;

public abstract class IFunctionContext : IFunctionCaller
{
    public abstract void AddLocal(FunctionVariable variable, bool usedElsewhere);
}
