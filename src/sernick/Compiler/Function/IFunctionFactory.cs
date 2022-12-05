namespace sernick.Compiler.Function;

public interface IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyList<IFunctionParam> parameters, bool returnsValue);
}
