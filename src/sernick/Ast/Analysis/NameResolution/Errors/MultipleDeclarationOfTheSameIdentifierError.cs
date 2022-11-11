namespace sernick.Ast.Analysis.NameResolution.Errors;

using Diagnostics;
using Nodes;

public record MultipleDeclarationOfTheSameIdentifierError(Declaration Original, Declaration Repeat) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
    
    public override string ToString()
    {
        return $"Multiple declarations of identifier: {Original}, {Repeat}";
    }
}
