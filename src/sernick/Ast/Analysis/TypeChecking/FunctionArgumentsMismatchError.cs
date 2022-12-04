namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record FunctionArgumentsMismatchError(int Expected, int Actual, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Wrong number of arguments in a function call: expected \"{Expected}\", provided \"{Actual}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
