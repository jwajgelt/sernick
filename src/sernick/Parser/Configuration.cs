namespace sernick.Parser;

using Common.Dfa;
using Common.Regex;
using Grammar.Dfa;
using Grammar.Syntax;

public sealed record Configuration<TSymbol>(
    IReadOnlySet<ValueTuple<SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>.State, TSymbol>> States
) where TSymbol : IEquatable<TSymbol>
{
    public Configuration<TSymbol> Closure(DfaGrammar<TSymbol> dfaGrammar)
    {
        HashSet<ValueTuple<SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>.State, TSymbol>> states = new(States);

        bool hasChanged;
        do
        {
            hasChanged = false;
            foreach (var (state, symbol) in States)
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
