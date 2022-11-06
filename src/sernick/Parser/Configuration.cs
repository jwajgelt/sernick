namespace sernick.Parser;

using Common.Dfa;
using Common.Regex;
using Grammar.Dfa;
using Grammar.Syntax;

public sealed record Configuration<TSymbol>(
    IReadOnlySet<ValueTuple<SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>.State, TSymbol>> States
) where TSymbol : IEquatable<TSymbol>
{
    public static Configuration<TSymbol> Closure(HashSet<ValueTuple<SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>.State, TSymbol>> states, DfaGrammar<TSymbol> dfaGrammar)
    {

        bool hasChanged;
        do
        {
            hasChanged = false;
            foreach (var (state, symbol) in states)
            {
                foreach (var edge in dfaGrammar.Productions[symbol].GetTransitionsFrom(state))
                {
                    hasChanged |= states.Add((dfaGrammar.Productions[edge.Atom].Start, symbol));
                }
            }
        } while (hasChanged);

        return new Configuration<TSymbol>(states);
    }
}
