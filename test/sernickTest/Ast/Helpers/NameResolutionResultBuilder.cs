namespace sernickTest.Ast.Helpers;

using Castle.Core;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;

public static class NameResolutionResultBuilder
{
    public static NameResolutionResult NameResolution() => new NameResolutionResult();

    public static NameResolutionResult WithVars(this NameResolutionResult nameResolution,
        params (VariableValue, Declaration)[] vars) =>
        nameResolution with
        {
            UsedVariableDeclarations = vars.ToDictionary(
                variable => variable.Item1,
                variable => variable.Item2,
                ReferenceEqualityComparer<VariableValue>.Instance)
        };

    public static NameResolutionResult WithCalls(this NameResolutionResult nameResolution,
        params (FunctionCall, FunctionDefinition)[] calls) =>
        nameResolution with
        {
            CalledFunctionDeclarations = calls.ToDictionary(
                call => call.Item1,
                call => call.Item2,
                ReferenceEqualityComparer<FunctionCall>.Instance)
        };
}
