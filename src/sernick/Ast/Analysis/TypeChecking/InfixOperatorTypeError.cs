namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;
using Nodes.Conversion;

public sealed record InfixOperatorTypeError(Type lhsType, Type rhsType, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Cannot apply an infix operator with types: ${lhsType} and ${rhsType}: at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
