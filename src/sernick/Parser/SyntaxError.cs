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
                : $"Syntax error: unexpected symbol {NextParseNode?.Symbol?.ToString()} beginning at {NextParseNode?.LocationRange.Start} and ending at {NextParseNode?.LocationRange.End}.";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;

    public bool Equals(SyntaxError<TSymbol>? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => NextParseNode?.GetHashCode() ?? 0;
}
