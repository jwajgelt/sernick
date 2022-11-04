namespace sernickTest.Grammar.Helpers;

using sernick.Grammar.Dfa;
using sernick.Grammar.Syntax;
using sernickTest.Common.Dfa.Helpers;

public static class DfaGrammarExtensions
{
    /// <returns>Sorted IEnumerable with Left symbols in dfa grammar productions</returns>
    public static IEnumerable<char> GetLeftSymbols(this DfaGrammar<char> dfaGrammar) =>
        dfaGrammar.Productions.Keys.OrderBy(symbol => symbol);

    /// <returns>IEnumerable of every accepting production sorted by Left symbol</returns>
    public static IEnumerable<Production<char>> AcceptingProductions(this DfaGrammar<char> dfaGrammar, string text)
    {
        return dfaGrammar.Productions.Values
            .SelectMany(sumDfa => sumDfa.AcceptingCategories(sumDfa.Transition(sumDfa.Start, text)))
            .OrderBy(production => production.Left);
    }
}
