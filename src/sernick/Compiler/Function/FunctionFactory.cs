namespace sernick.Compiler.Function;

public sealed class FunctionFactory : IFunctionFactory
{
    private int _contextCounter = 0;
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyCollection<IFunctionParam> parameters, bool returnsValue)
    {
        return new FunctionContext(parent, parameters, returnsValue, _contextCounter++);
    }
}
