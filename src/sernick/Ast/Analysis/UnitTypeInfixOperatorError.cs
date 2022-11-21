namespace sernick.Ast.Analysis;

using Diagnostics;
using Input;

public sealed record UnitTypeInfixOperatorError( ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Cannot appy an infix operator if one of the arguments' type is Unit: at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
