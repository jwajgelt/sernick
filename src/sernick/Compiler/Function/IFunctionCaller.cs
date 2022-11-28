namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public interface IFunctionCaller
{
    public GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeNode> arguments);

    public record GenerateCallResult(IReadOnlyList<CodeTreeNode> CodeGraph, CodeTreeValueNode? ResultLocation);
}
