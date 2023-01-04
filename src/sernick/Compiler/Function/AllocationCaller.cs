using sernick.CodeGeneration;
using sernick.ControlFlowGraph.CodeTree;

namespace sernick.Compiler.Function;

public sealed class AllocationCaller : IFunctionCaller
{
    public Label Label => "new";

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        throw new NotImplementedException();
    }
}