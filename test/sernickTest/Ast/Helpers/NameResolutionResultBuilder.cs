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

    public static NameResolutionResult WithAssigns(this NameResolutionResult nameResolution,
        params (Assignment, VariableDeclaration)[] assigns) =>
        nameResolution with
        {
            AssignedVariableDeclarations = assigns.ToDictionary(
                variable => variable.Item1,
                variable => variable.Item2,
                ReferenceEqualityComparer<Assignment>.Instance)
        };
}
