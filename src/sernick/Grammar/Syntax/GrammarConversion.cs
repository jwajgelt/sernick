namespace sernick.Grammar.Syntax;

using Common.Dfa;
using Common.Regex;
using Dfa;

public static class GrammarConversion
{
    /// <summary>
    /// Converts regex-based syntax grammar to DFA-based one.
    /// Usage: grammar.ToDfaGrammar()
    /// </summary>
    /// <returns>DFA-based grammar equivalent to the input one</returns>
    public static DfaGrammar<TSymbol, Regex> ToDfaGrammar<TSymbol>(this Grammar<TSymbol> grammar)
        where TSymbol : notnull
    {
        return new DfaGrammar<TSymbol, Regex>(
            Start: grammar.Start,
            Productions: grammar.Productions
                .ToDictionary(
                    prod => prod.Left,
                    prod => new RegexDfa(prod.Right) as IDfaWithConfig<Regex>)
            );
    }
}
