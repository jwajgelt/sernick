namespace sernick.Compiler.Function;

using Ast.Nodes;

public interface IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyList<IFunctionParam> parameters, bool returnsValue, Identifier name);
}
