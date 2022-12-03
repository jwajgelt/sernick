namespace sernick.Ast.Analysis.ControlFlowGraph;

using Compiler.Function;
using FunctionContextMap;
using NameResolution;
using Nodes;
using sernick.ControlFlowGraph.CodeTree;

public static class ControlFlowAnalyzer
{
    public static CodeTreeRoot UnravelControlFlow(
        AstNode root,
        NameResolutionResult nameResolution,
        FunctionContextMap contextMap,
        Func<AstNode, NameResolutionResult, IFunctionContext, IReadOnlyList<SingleExitNode>> pullOutSideEffects)
    {
        throw new NotImplementedException();
    }
}
