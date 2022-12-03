namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record ReturnTypeError(Type Required, Type Provided, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Return expression has bad type: required \"{Required}\", provided \"{Provided}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
