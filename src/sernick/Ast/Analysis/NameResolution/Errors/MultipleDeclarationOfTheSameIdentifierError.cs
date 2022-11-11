namespace sernick.Ast.Analysis.NameResolution.Errors;

using Diagnostics;
using Nodes;

public record MultipleDeclarationOfTheSameIdentifierError(Identifier Identifier) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
    
    public override string ToString()
    {
        return $"Multiple declarations of identifier: {Identifier}";
    }
}
