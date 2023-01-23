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

    private readonly int _memoryToAllocBytes;
    public MemcpyCaller(int memoryToAllocBytes)
    {
        _memoryToAllocBytes = memoryToAllocBytes;
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
        var pushRsp = Reg(rsp).Write(rspRead - POINTER_SIZE);

        operations.Add(pushRsp);

        // call malloc first, then call memcpy
        var (mallocCallOperations, ptrToMemoryAllocatedByMalloc) =
            new MallocCaller().GenerateCall(new CodeTreeValueNode[] { _memoryToAllocBytes });
        operations.AddRange(mallocCallOperations.SelectMany(node => node.Operations));

        // Arguments:
        // void *dest
        operations.Add(Reg(HardwareRegister.RDI).Write(ptrToMemoryAllocatedByMalloc!));
        // const void* src
        operations.Add(Reg(HardwareRegister.RSI).Write(arguments.Single()));
        // size_t n -- struct size
        operations.Add(Reg(HardwareRegister.RDX).Write(new
            sernick.ControlFlowGraph.CodeTree.Constant(
            new RegisterValue(_memoryToAllocBytes)
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
