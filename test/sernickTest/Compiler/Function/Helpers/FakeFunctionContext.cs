namespace sernickTest.Compiler.Function.Helpers;

using Castle.Core;
using sernick.CodeGeneration;
using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;

public sealed class FakeFunctionContext : IFunctionContext
{
    private readonly Dictionary<IFunctionVariable, bool> _locals = new(ReferenceEqualityComparer<IFunctionVariable>.Instance);

    public IReadOnlyDictionary<IFunctionVariable, bool> Locals => _locals;

    public Label Label => "fake";

    public int Depth => 0;
    public bool ValueIsReturned => false;
    public IReadOnlyDictionary<IFunctionVariable, int> LocalVariableSize => new Dictionary<IFunctionVariable, int>();

    public IReadOnlyDictionary<IFunctionVariable, bool> LocalVariableIsStruct =>
        new Dictionary<IFunctionVariable, bool>();

    public void AddLocal(IFunctionVariable variable, int size, bool isStruct, bool usedElsewhere) => _locals[variable] = usedElsewhere;
    public IReadOnlyList<SingleExitNode> GeneratePrologue()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<SingleExitNode> GenerateEpilogue(CodeTreeValueNode? valToReturn)
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

    public VariableLocation AllocateStackFrameSlot()
    {
        throw new NotImplementedException();
    }

    CodeTreeValueNode IFunctionContext.GetIndirectVariableLocation(IFunctionVariable variable)
    {
        throw new NotImplementedException();
    }

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        throw new NotImplementedException();
    }

    bool IFunctionContext.IsVariableStruct(IFunctionVariable variable)
    {
        // for now
        return false;
    }
}

public record FakeVariableRead(IFunctionVariable Variable) : CodeTreeValueNode;

public record FakeVariableWrite(IFunctionVariable Variable, CodeTreeValueNode Value) : CodeTreeNode;
