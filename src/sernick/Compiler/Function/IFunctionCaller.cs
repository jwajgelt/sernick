namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public interface IFunctionCaller
{
    public GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments);

    public record GenerateCallResult(IReadOnlyList<SingleExitNode> CodeGraph, CodeTreeNode? ResultLocation);
}
