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
        var dfaProductions = grammar.Productions.GroupBy(production => production.Left)
            .Select(group => (
                symbol: group.Key,
                dfas: group.ToDictionary(
                    prod => prod,
                    prod => RegexDfa<TSymbol>.FromRegex(prod.Right))))
            .ToDictionary(
                item => item.symbol,
                item => new SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>(item.dfas));

        return new DfaGrammar<TSymbol>(
            Start: grammar.Start,
            Productions: dfaProductions
            );
    }
}
