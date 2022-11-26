namespace sernick.Compiler.Function;

public interface IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyCollection<IFunctionParam> parameters, bool returnsValue);
}
