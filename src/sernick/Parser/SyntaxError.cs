namespace sernick.Parser;

using Diagnostics;
using ParseTree;

public sealed record SyntaxError<TSymbol>(IParseTree<TSymbol>? ParseNode) : IDiagnosticItem
{
    public bool Equals(IDiagnosticItem? other) => other is SyntaxError<TSymbol> && other.ToString() == ToString();

    public override string ToString()
    {
        return ParseNode is not null ?
            $"Syntax error: unexpected symbol \"{ParseNode.Symbol}\" at {ParseNode.Start}" :
            "Syntax error: unexpected EOF";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
