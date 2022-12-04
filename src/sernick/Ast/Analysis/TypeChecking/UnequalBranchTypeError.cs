namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record UnequalBranchTypeError(Type TrueBranchType, Type FalseBranchType, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"True/false branch in If statement should have the same type, but they do not: true branch of type \"{TrueBranchType}\", false branch of type \"{FalseBranchType}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
