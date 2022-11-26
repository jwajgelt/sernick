namespace sernick.Compiler.Function;

public sealed class FunctionFactory : IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyCollection<IFunctionParam> parameters, bool returnsValue)
    {
        return new FunctionContext(parent, parameters, returnsValue);
    }
}
