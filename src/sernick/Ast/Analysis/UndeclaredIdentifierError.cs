namespace sernick.Ast.Analysis;

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
