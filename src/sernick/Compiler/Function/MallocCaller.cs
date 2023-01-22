namespace sernick.Compiler.Function;
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
        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(GetOperations(arguments)), null);
    }

    public List<CodeTreeNode> GetOperations(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        var operations = new List<CodeTreeNode>();

        Register rsp = HardwareRegister.RSP;
        var rspRead = Reg(rsp).Read();
        _ = Reg(rsp).Write(rspRead - POINTER_SIZE);

        // Arguments (one):
        // size_t size
        operations.Add(Reg(HardwareRegister.RDI).Write(arguments.Single()));

        // Align the stack
        var tmpRsp = Reg(new Register());
        operations.Add(tmpRsp.Write(rspRead));
        operations.Add(Reg(rsp).Write(rspRead & -2 * POINTER_SIZE));

        // Performing actual call (puts return address on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Restore stack pointer
        operations.Add(Reg(rsp).Write(tmpRsp.Read()));

        return operations;
    }
}
