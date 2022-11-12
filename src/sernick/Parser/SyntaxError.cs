namespace sernick.Parser;

using Diagnostics;
using ParseTree;

public sealed record SyntaxError<TSymbol>
    (IParseTree<TSymbol>? NextParseNode) : IDiagnosticItem
{
    public override string ToString()
    {
        return NextParseNode == null || NextParseNode!.Symbol == null 
                ? "Syntax error: unexpected symbol EOF."
                : $"Syntax error: unexpected symbol {NextParseNode?.Symbol?.ToString()} beginning at {NextParseNode?.Start} and ending at {NextParseNode?.End}.";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
