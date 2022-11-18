namespace sernickTest.Compiler.Function.Helpers;

using sernick.Compiler.Function;

public sealed class FakeFunctionContext : IFunctionContext
{
    private readonly Dictionary<FunctionVariable, bool> _locals = new();

    public IReadOnlyDictionary<FunctionVariable, bool> Locals => _locals;

    public void AddLocal(FunctionVariable variable, bool usedElsewhere) => _locals[variable] = usedElsewhere;
}
