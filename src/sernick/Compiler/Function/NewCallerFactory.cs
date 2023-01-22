namespace sernick.Compiler.Function;

using sernick.CodeGeneration;
using sernick.ControlFlowGraph.CodeTree;
using static Compiler.PlatformConstants;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Helpers;


public sealed class NewCallerFactory
{
    public NewCallerFactory()
    {
    }

    public static MemcpyCaller GetMemcpyCaller(int StructSize)
    {
        return new MemcpyCaller(StructSize);
    }

}
