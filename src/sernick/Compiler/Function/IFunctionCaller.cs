namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;
using Instruction;

public interface IFunctionCaller
{
    public Label Label { get; }

    public GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeNode> arguments);

    public record GenerateCallResult(IReadOnlyList<CodeTreeNode> CodeGraph, CodeTreeNode? ResultLocation);
}
