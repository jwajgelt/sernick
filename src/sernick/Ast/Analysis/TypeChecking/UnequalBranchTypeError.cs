namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record UnequalBranchTypeError(Type trueBranchType, Type falseBranchType, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"True/false branch in If statement should have the same type, but they do not: true branch of type \"{trueBranchType}\", false branch of type \"{falseBranchType}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
