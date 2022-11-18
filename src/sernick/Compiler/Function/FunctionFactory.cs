namespace sernick.Compiler.Function;

public sealed class FunctionFactory : IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyCollection<FunctionParam> parameters, bool returnsValue)
    {
        return new FunctionContext();
    }
}
