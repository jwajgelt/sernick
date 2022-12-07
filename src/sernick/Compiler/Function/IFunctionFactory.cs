namespace sernick.Compiler.Function;

using Ast.Nodes;

public interface IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, Identifier name,
        IReadOnlyList<IFunctionParam> parameters, bool returnsValue);
}
