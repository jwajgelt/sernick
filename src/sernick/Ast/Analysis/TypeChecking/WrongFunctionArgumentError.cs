namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;

public sealed record WrongFunctionArgumentError(Type Required, Type Provided, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Wrong function argument type \"{Required}\", provided \"{Provided}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
