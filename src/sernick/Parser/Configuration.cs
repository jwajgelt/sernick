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
            foreach (var (state, symbol) in statesSet.ToList())
            {
                foreach (var edge in dfaGrammar.Productions[symbol].GetTransitionsFrom(state))
                {
                    if (dfaGrammar.Productions.TryGetValue(edge.Atom, out var dfa))
                    {
                        hasChanged |= statesSet.Add((dfa.Start, edge.Atom));
                    }
                }
            }
        } while (hasChanged);

        return new Configuration<TSymbol>(statesSet);
    }

    public bool Equals(Configuration<TSymbol>? other)
    {
        return other is not null &&
               States.Count == other.States.Count &&
               States.Zip(other.States)
                   .All(statePair => statePair.First.Equals(statePair.Second));
    }

    public override int GetHashCode()
    {
        return States.Aggregate(0, (hashCode, state) => hashCode ^ state.GetHashCode());
    }

    public override string ToString() => string.Join(", ", States);
}
