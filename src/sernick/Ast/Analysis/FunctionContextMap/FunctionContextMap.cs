namespace sernick.Ast.Analysis.FunctionContextMap;

using Compiler.Function;
using Nodes;

public sealed class FunctionContextMap
{
    private readonly Dictionary<FunctionDefinition, IFunctionContext> _implementations = new(ReferenceEqualityComparer.Instance);
    public IReadOnlyDictionary<FunctionDefinition, IFunctionContext> Implementations => _implementations;

    public IFunctionContext this[FunctionDefinition functionDefinition]
    {
        get => _implementations[functionDefinition];
        set => _implementations[functionDefinition] = value;
    }

    private readonly Dictionary<FunctionCall, IFunctionCaller> _callers = new(ReferenceEqualityComparer.Instance);
    public IReadOnlyDictionary<FunctionCall, IFunctionCaller> Callers => _callers;

    public IFunctionCaller this[FunctionCall functionCall]
    {
        get => _callers[functionCall];
        set => _callers[functionCall] = value;
    }
}
