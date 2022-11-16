namespace sernick.Compiler.Function;

public abstract record FunctionParam;

public sealed class FunFactory
{
    public IFunctionContext MoreFun(IFunctionContext? parent, IReadOnlyCollection<FunctionParam> parameters, bool result)
    {
        throw new NotImplementedException();
    }
}
