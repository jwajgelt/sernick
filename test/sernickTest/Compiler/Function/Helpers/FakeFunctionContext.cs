namespace sernickTest.Compiler.Function.Helpers;

using Castle.Core;
using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;

public sealed class FakeFunctionContext : IFunctionContext
{
    private readonly Dictionary<IFunctionVariable, bool> _locals = new(ReferenceEqualityComparer<IFunctionVariable>.Instance);

    public IReadOnlyDictionary<IFunctionVariable, bool> Locals => _locals;

    public int Depth { get; }

    public void AddLocal(IFunctionVariable variable, bool usedElsewhere) => _locals[variable] = usedElsewhere;
    public IReadOnlyList<SingleExitNode> GeneratePrologue()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<SingleExitNode> GenerateEpilogue()
    {
        throw new NotImplementedException();
    }

    public CodeTreeValueNode GenerateVariableRead(IFunctionVariable variable)
    {
        return new FakeVariableRead(variable);
    }

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value)
    {
        return new FakeVariableWrite(variable, value);
    }

    CodeTreeValueNode IFunctionContext.GetIndirectVariableLocation(IFunctionVariable variable)
    {
        throw new NotImplementedException();
    }

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        throw new NotImplementedException();
    }
}

public record FakeVariableRead(IFunctionVariable Variable) : CodeTreeValueNode;

public record FakeVariableWrite(IFunctionVariable Variable, CodeTreeValueNode Value) : CodeTreeNode;
