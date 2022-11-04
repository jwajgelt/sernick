namespace sernick.Utility;

using Common.Dfa;
using Common.Regex;
using Grammar.Dfa;
using Grammar.Syntax;

public static class DfaGrammarExtensions
{
    public static Dictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>> GetReverseAutomatas<TSymbol>(this DfaGrammar<TSymbol> dfaGrammar) where TSymbol : IEquatable<TSymbol>
    {
        var reversedAutomatas = new Dictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>>();
        foreach (var sumDfa in dfaGrammar.Productions)
        {
            foreach (var pair in sumDfa.Value.Dfas)
            {
                reversedAutomatas[pair.Key] = ((RegexDfa<TSymbol>)pair.Value).Reverse();
            }
        }

        return reversedAutomatas;
    }

    private static IDfa<Regex<TAtom>, TAtom> Reverse<TAtom>(this RegexDfa<TAtom> dfa) where TAtom : IEquatable<TAtom>
    {
        return RegexDfa<TAtom>.FromRegex(dfa.Start.Reverse());
    }
}
