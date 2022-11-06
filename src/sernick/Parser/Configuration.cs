namespace sernick.Parser;

using Common.Dfa;
using Common.Regex;
using Grammar.Dfa;
using Grammar.Syntax;

public sealed record Configuration<TSymbol>(
    IReadOnlySet<ValueTuple<SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>.State, TSymbol>> States
) where TSymbol : IEquatable<TSymbol>
{
    public static Configuration<TSymbol> Closure(
        IEnumerable<ValueTuple<SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>.State, TSymbol>> states,
        DfaGrammar<TSymbol> dfaGrammar)
    {
        var statesSet = states.ToHashSet();

        bool hasChanged;
        do
        {
            hasChanged = false;
            foreach (var (state, symbol) in statesSet)
            {
                foreach (var edge in dfaGrammar.Productions[symbol].GetTransitionsFrom(state))
                {
                    if (dfaGrammar.Productions.TryGetValue(edge.Atom, out var dfa))
                    {
                        hasChanged |= statesSet.Add((dfa.Start, symbol));
                    }
                }
            }
        } while (hasChanged);

        return new Configuration<TSymbol>(statesSet);
    }
}
