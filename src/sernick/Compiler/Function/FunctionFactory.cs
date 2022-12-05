namespace sernick.Compiler.Function;

public sealed class FunctionFactory : IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyList<IFunctionParam> parameters, bool returnsValue)
    {
        return new FunctionContext(parent, parameters, returnsValue);
    }
}
