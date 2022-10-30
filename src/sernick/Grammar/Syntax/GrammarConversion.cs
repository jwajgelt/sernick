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
    public static DfaGrammar<TSymbol> ToDfaGrammar<TSymbol>(this Grammar<TSymbol> grammar)
        where TSymbol : IEquatable<TSymbol>
    {
        var dfaProductions = grammar.Productions.Select(production => (
                production.Left,
                RegexDfa<TSymbol>.FromRegex(production.Right),
                production
            )
        ).GroupBy(prod => prod.Left).Select(prodGroup =>
            (prodGroup.Key, new SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>(
                prodGroup.ToDictionary(item => item.production, item => item.Item2)
            ))
        ).ToDictionary(pair => pair.Key, pair => pair.Item2);

        return new DfaGrammar<TSymbol>(
            Start: grammar.Start,
            Productions: dfaProductions
            );
    }
}
