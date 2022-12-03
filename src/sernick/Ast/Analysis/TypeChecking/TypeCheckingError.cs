namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record TypeCheckingError(Type Required, Type Provided, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Type checking error: required \"{Required}\", provided \"{Provided}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}