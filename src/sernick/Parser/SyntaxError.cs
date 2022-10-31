namespace sernick.Parser;

using Diagnostics;
using ParseTree;

public sealed record SyntaxError<TSymbol>(IParseTree<TSymbol>? ParseNode) : IDiagnosticItem
{
    public override string ToString()
    {
        return ParseNode is not null ?
            $"Syntax error: unexpected symbol {ParseNode.Symbol} at {ParseNode.Start}" :
            "Syntax error: unexpected EOF";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
