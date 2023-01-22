namespace sernick.Compiler.Function;

using System.Reflection.Metadata;
using sernick.CodeGeneration;
using sernick.ControlFlowGraph.CodeTree;
using static Compiler.PlatformConstants;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Helpers;


public sealed class MallocCaller : IFunctionCaller
{
    // void *malloc(size_t size)
    public Label Label { get; } = "malloc";

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(this.GetOperations(arguments)), null);
    }

    public List<CodeTreeNode> GetOperations(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        var operations = new List<CodeTreeNode>();

        // Arguments (one):
        // size_t size
        operations.Add(Reg(HardwareRegister.RDI).Write(arguments.Single()));

        // Performing actual call (puts return address on stack and jumps)
        operations.Add(new FunctionCall(this));

        return operations;
    }
}
