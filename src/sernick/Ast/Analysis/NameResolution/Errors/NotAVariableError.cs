namespace sernick.Ast.Analysis.NameResolution.Errors;

using Diagnostics;
using Nodes;

public record NotAVariableError(Identifier Identifier) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
    
    public override string ToString()
    {
        return $"Identifier does not represent a variable: {Identifier}";
    }
}
