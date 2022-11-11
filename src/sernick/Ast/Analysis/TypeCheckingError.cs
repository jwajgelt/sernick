namespace sernick.Ast.Analysis;

using Diagnostics;
using Input;

public sealed record TypeCheckingError(Type Required, Type Provided, ILocation Location) : IDiagnosticItem
{
    public bool Equals(IDiagnosticItem? other) => other is TypeCheckingError && other.ToString() == ToString();

    public override string ToString()
    {
        return $"Type checking error: required \"{Required}\", provided \"{Provided}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
