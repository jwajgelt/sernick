namespace sernick.Ast.Analysis.NameResolution.Errors;

using Diagnostics;
using Nodes;

public record UndeclaredIdentifierError(Identifier Identifier) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;

    public override string ToString()
    {
        return $"Undeclared identifier: {Identifier}";
    }
}