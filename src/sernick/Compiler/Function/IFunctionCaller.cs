namespace sernick.Compiler.Function;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public interface IFunctionCaller
{
    // NOTE: temporary until we decide how we generate function labels
    public Label Label { get; }

    public GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments);

    public record GenerateCallResult(IReadOnlyList<SingleExitNode> CodeGraph, CodeTreeValueNode? ResultLocation);
}
