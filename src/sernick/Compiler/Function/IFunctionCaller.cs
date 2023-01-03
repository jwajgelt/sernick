namespace sernick.Compiler.Function;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public interface IFunctionCaller
{
    public Label Label { get; }

    public GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments);

    public sealed record GenerateCallResult(IReadOnlyList<SingleExitNode> CodeGraph, CodeTreeValueNode? ResultLocation);
}
