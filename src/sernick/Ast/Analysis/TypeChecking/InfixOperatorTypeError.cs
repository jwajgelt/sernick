namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;
using sernick.Ast.Nodes;

public sealed record InfixOperatorTypeError(Infix.Op Operator, Type LhsType, Type RhsType, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Cannot apply an infix operator ${Operator} with types: ${LhsType} and ${RhsType}: at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
