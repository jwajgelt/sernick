namespace sernick.Parser;

using Diagnostics;
using ParseTree;

public sealed record SyntaxError<TSymbol>
    (IParseTree<TSymbol>? PreviousParseNode, IParseTree<TSymbol>? NextParseNode) : IDiagnosticItem
{
    public override string ToString()
    {
        return
            $"Syntax error: unexpected symbol {NextParseNode?.Symbol?.ToString() ?? "EOF"}. Last parsed symbol: {PreviousParseNode?.ToString()}.";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
