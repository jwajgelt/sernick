namespace sernick.Compiler.Function;

public abstract record FunctionParam;

public interface IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyCollection<FunctionParam> parameters, bool returnsValue);
}
