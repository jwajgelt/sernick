namespace sernick.CodeGeneration;

using ControlFlowGraph.CodeTree;

public sealed class DisplayTable : IAsmable
{
    public const string DISPLAY_TABLE_SYMBOL = "__display_table";

    private long Size { get; }

    public DisplayTable(long size)
    {
        Size = size;
    }

    public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> _)
    {
        return @$"
section .bss
    {DISPLAY_TABLE_SYMBOL} resq {Size}
";
    }
}
