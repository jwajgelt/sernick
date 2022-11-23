namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public interface IFunctionCaller
{
    public (IReadOnlyList<CodeTreeNode> codeGraph, CodeTreeNode? resultLocation) GenerateCall(IReadOnlyList<CodeTreeNode> arguments);
}
