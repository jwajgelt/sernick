namespace sernick.Compiler.Function;

using System.Reflection.Metadata;
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
        this.MemoryToAllocBytes = memoryToAllocBytes;
    }

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new Exception("'New' should have exactly one argument.");
        }

        var operations = new List<CodeTreeNode>();

        // call malloc first, then call memcpy
        var mallocCallOperations = new MallocCaller().GetOperations(new CodeTreeValueNode[] { this.MemoryToAllocBytes });
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
            new RegisterValue(this.MemoryToAllocBytes)
           )
        ));


        // Performing actual call (puts return address on stack and jumps)
        operations.Add(new FunctionCall(this));

        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations), null);
    }
}
