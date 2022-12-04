namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;
using sernick.Ast.Nodes;

public sealed record InfixOperatorTypeError(Infix.Op _operator, Type lhsType, Type rhsType, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Cannot apply an infix operator ${_operator} with types: ${lhsType} and ${rhsType}: at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
