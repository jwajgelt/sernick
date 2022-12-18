namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public static class Convention
{
    public static readonly HardwareRegister[] CallerToSave = {
        HardwareRegister.R8,
        HardwareRegister.R9,
        HardwareRegister.R10,
        HardwareRegister.R11,
        HardwareRegister.RAX,
        HardwareRegister.RCX,
        HardwareRegister.RDX,
    };

    public static readonly HardwareRegister[] CalleeToSave = {
        HardwareRegister.R12,
        HardwareRegister.R13,
        HardwareRegister.R14,
        HardwareRegister.R15,
        HardwareRegister.RBX,
        HardwareRegister.RDI,
        HardwareRegister.RSI,
    };

    public static readonly HardwareRegister[] ArgumentRegisters = {
        HardwareRegister.RDI,
        HardwareRegister.RSI,
        HardwareRegister.RDX,
        HardwareRegister.RCX,
        HardwareRegister.R8,
        HardwareRegister.R9,
    };

    public static readonly int REG_ARGS_COUNT = ArgumentRegisters.Length;
}

public static class Helpers
{
    public static List<SingleExitNode> CodeTreeListToSingleExitList(List<CodeTreeNode> trees)
    {
        var result = new List<SingleExitNode>();
        trees.Reverse();
        SingleExitNode? nextRoot = null;
        foreach (var tree in trees)
        {
            var treeRoot = new SingleExitNode(nextRoot, new List<CodeTreeNode> { tree });
            result.Add(treeRoot);
            nextRoot = treeRoot;
        }

        result.Reverse();
        return result;
    }
}
