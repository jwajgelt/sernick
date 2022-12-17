namespace sernick.CodeGeneration;

using ControlFlowGraph.CodeTree;

public class DisplayTable : IAsmable
{
    private long Size { get; }

    public DisplayTable(long size)
    {
        Size = size;
    }

    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping)
    {
        return @$"
section .bss
__display_table resq {Size}
";
    }
}
