namespace sernick.Parser;

using Grammar.Syntax;

public sealed class NotSLRGrammarException : Exception
{
    private NotSLRGrammarException(string message) : base(message) { }

    public static NotSLRGrammarException ShiftReduceConflict<TSymbol>(TSymbol? symbol, Production<TSymbol> production)
        where TSymbol : IEquatable<TSymbol>
    {
        return new NotSLRGrammarException(
            $"Shift/reduce conflict between symbol {symbol?.ToString() ?? "EOF"} and production {production}");
    }

    public static NotSLRGrammarException ReduceReduceConflict<TSymbol>(
        TSymbol symbol,
        Production<TSymbol> production1,
        Production<TSymbol> production2)
        where TSymbol : IEquatable<TSymbol>
    {
        return new NotSLRGrammarException(
            $"Reduce/reduce conflict for symbol {symbol} between production {production1} and production {production2}");
    }
}
