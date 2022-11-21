namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record FunctionArgumentsMismatchError(int expected, int actual, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Wrong number of arguments in a function call: expected \"{expected}\", provided \"{actual}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
