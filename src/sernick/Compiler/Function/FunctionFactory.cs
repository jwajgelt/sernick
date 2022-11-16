namespace sernick.Compiler.Function;

public sealed class FunctionFactory : IFunctionFactory
{
    public IFunctionContext MoreFun(IFunctionContext? parent, IReadOnlyCollection<FunctionParam> parameters, bool hasResult)
    {
        return new FunctionContext();
    }
}
