namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record UnitTypeNotAllowedInFunctionArgumentError(ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Unit type is not allowed as a function argument type: at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
