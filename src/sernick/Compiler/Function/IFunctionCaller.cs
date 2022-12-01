namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;
using Instruction;

public interface IFunctionCaller
{
    public Label Label { get; }

    public GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments);

    public record GenerateCallResult(IReadOnlyList<SingleExitNode> CodeGraph, CodeTreeValueNode? ResultLocation);
}
