namespace sernick.Utility;

using Common.Dfa;
using Common.Regex;
using Grammar.Dfa;
using Grammar.Syntax;
using Parser;

public static class DfaGrammarExtensions
{
    public static IReadOnlyDictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>> GetReverseAutomatas<TSymbol>(
        this DfaGrammar<TSymbol> dfaGrammar)
        where TSymbol : IEquatable<TSymbol>
    {
        return dfaGrammar.Productions.Values
            .SelectMany(sumDfa => sumDfa.Dfas)
            .Select(kv => (production: kv.Key, dfa: kv.Value))
            .ToDictionary(
                item => item.production,
                item => ((RegexDfa<TSymbol>)item.dfa).Reverse());
    }

    private static IDfa<Regex<TAtom>, TAtom> Reverse<TAtom>(this RegexDfa<TAtom> dfa) where TAtom : IEquatable<TAtom>
    {
        return RegexDfa<TAtom>.FromRegex(dfa.Start.Reverse());
    }

    public static DfaGrammar<TSymbol> WithDummyStartSymbol<TSymbol>(this DfaGrammar<TSymbol> dfaGrammar, TSymbol dummyStart)
        where TSymbol : IEquatable<TSymbol>
    {
        var startProduction = new Production<TSymbol>(dummyStart, Regex<TSymbol>.Atom(dfaGrammar.Start));
        var productions =
            new Dictionary<TSymbol, SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>>(dfaGrammar.Productions)
            {
                [dummyStart] = new(new Dictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>>
                {
                    [startProduction] = RegexDfa<TSymbol>.FromRegex(startProduction.Right)
                })
            };
        return new DfaGrammar<TSymbol>(dummyStart, productions);
    }
}
