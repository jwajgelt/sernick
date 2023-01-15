namespace sernick.Compiler.Function;

using Ast.Nodes;

public interface IFunctionFactory
{
    public IFunctionContext CreateFunction(IFunctionContext? parent, Identifier name, int? distinctionNumber,
        IReadOnlyList<IFunctionParam> parameters, bool returnsValue, bool returnsStruct = false);
}
