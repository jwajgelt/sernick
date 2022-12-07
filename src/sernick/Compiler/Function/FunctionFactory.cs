namespace sernick.Compiler.Function;

using Ast.Nodes;
using GenerateLabel = LabelGenerator.GenerateLabel;

public sealed class FunctionFactory : IFunctionFactory
{
    private readonly GenerateLabel _generateLabel;

    public FunctionFactory(GenerateLabel generateLabel)
    {
        _generateLabel = generateLabel;
    }

    public IFunctionContext CreateFunction(IFunctionContext? parent, IReadOnlyList<IFunctionParam> parameters, bool returnsValue, Identifier name)
    {
        return new FunctionContext(parent, parameters, returnsValue, _generateLabel(parent, name));
    }
}
