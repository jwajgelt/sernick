namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record TypeOrInitialValueShouldBePresentError(ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Type or initial variable value have to be present. Found none at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
