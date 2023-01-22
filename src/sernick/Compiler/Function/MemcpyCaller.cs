namespace sernick.Compiler.Function;
using sernick.CodeGeneration;
using sernick.ControlFlowGraph.CodeTree;
using static Compiler.PlatformConstants;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Helpers;

public sealed class MemcpyCaller : IFunctionCaller
{
    // void *memcpy(void *dest, const void * src, size_t n)
    public Label Label { get; } = "memcpy";

    private readonly int MemoryToAllocBytes;
    public MemcpyCaller(int memoryToAllocBytes)
    {
        MemoryToAllocBytes = memoryToAllocBytes;
    }

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new Exception("'New' should have exactly one argument.");
        }

        var operations = new List<CodeTreeNode>();

        Register rsp = HardwareRegister.RSP;
        var rspRead = Reg(rsp).Read();
        _ = Reg(rsp).Write(rspRead - POINTER_SIZE);

        // call malloc first, then call memcpy
        var mallocCallOperations = new MallocCaller().GetOperations(new CodeTreeValueNode[] { MemoryToAllocBytes });
        operations.Concat(mallocCallOperations);

        // after calling malloc, the result will be in RAX
        var ptrToMemoryAllocatedByMalloc = Reg(HardwareRegister.RAX).Read();

        // Arguments:
        // void *dest
        operations.Add(Reg(HardwareRegister.RDI).Write(ptrToMemoryAllocatedByMalloc));
        // const void* src
        operations.Add(Reg(HardwareRegister.RSI).Write(arguments.Single()));
        // size_t n -- struct size
        operations.Add(Reg(HardwareRegister.RDX).Write(new
            sernick.ControlFlowGraph.CodeTree.Constant(
            new RegisterValue(MemoryToAllocBytes)
           )
        ));

        // Align the stack
        var tmpRsp = Reg(new Register());
        operations.Add(tmpRsp.Write(rspRead));
        operations.Add(Reg(rsp).Write(rspRead & -2 * POINTER_SIZE));

        // Performing actual call (puts return address on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Restore stack pointer
        operations.Add(Reg(rsp).Write(tmpRsp.Read()));

        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations), null);
    }
}
