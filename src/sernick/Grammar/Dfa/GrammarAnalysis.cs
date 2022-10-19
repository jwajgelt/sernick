namespace sernick.Grammar.Dfa;

public static class GrammarAnalysis
{
    /// <summary>
    /// Compute the set NULLABLE - all symbols, from which epsilon can be derived in grammar
    /// </summary>
    public static IReadOnlyCollection<TSymbol> Nullable<TSymbol, TDfaState>(
        this DfaGrammar<TSymbol, TDfaState> grammar)
        where TSymbol : IEquatable<TSymbol>
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compute the function FIRST(A) - all symbols that can appear as the first ones in grammar derivations starting at A
    /// </summary>
    /// <param name="nullableSymbols">Precomputed set NULLABLE</param>
    public static IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> First<TSymbol, TDfaState>(
        this DfaGrammar<TSymbol, TDfaState> grammar,
        IReadOnlyCollection<TSymbol> nullableSymbols)
        where TSymbol : IEquatable<TSymbol>
    {
        // TODO: get rid of `ToChar` once dfa is generic over symbol type
        var symbols = grammar.Productions.Select(kv => kv.Key).ToHashSet();

        // begin with FIRST(A) := {A}
        var result = grammar.Productions.ToDictionary(
            kv => kv.Key,
            kv => new HashSet<TSymbol>() { kv.Key }
        );

        // for each production, calculate the dfa states
        // reachable using only transitions with NULLABLE 
        // symbols, and for each transition leaving these states,
        // add it's symbol to FIRST(A)
        foreach (var (symbol, dfa) in grammar.Productions)
        {
            var reachable = new HashSet<TDfaState>() { dfa.Start };
            var processing = new HashSet<TDfaState>() { dfa.Start };

            while (processing.Count != 0)
            {
                var state = processing.First();

                foreach (var nullableSymbol in nullableSymbols)
                {
                    // TODO: get rid of `ToChar` once dfa is generic over symbol type
                    var next = dfa.Transition(state, nullableSymbol);
                    if (!reachable.Contains(next))
                    {
                        reachable.Add(next);
                        processing.Add(next);
                    }
                }

                processing.Remove(state);
            }

            var symbolsToAdd = processing
                .SelectMany(state => dfa.GetTransitionsFrom(state))
                .Select(transition => transition.Atom);

            result[symbol].UnionWith(symbolsToAdd);
            // add the symbols that only show up on the right side of productions
            symbols.UnionWith(symbolsToAdd);
        }

        foreach (var symbol in symbols.Where(symbol => !result.ContainsKey(symbol)))
        {
            result.Add(symbol, new HashSet<TSymbol>() { symbol });
        }

        // transitive closure over FIRST, using Floyd-Warshall
        // in each step, if u->v and v->w, add u->w
        foreach (var v in symbols)
        {
            foreach (var u in symbols)
            {
                foreach (var w in symbols)
                {
                    if (result[v].Contains(u) && result[w].Contains(v))
                    {
                        result[w].Add(u);
                    }
                }
            }
        }

        return (IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>>)result;
    }

    /// <summary>
    /// Compute the function FOLLOW(A) - all symbols that can appear immediately after A in grammar derivations starting at Start
    /// </summary>
    /// <param name="nullableSymbols">Precomputed set NULLABLE</param>
    /// <param name="symbolsFirst">Precomputed function FIRST(A)</param>
    public static IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> Follow<TSymbol, TDfaState>(
        this DfaGrammar<TSymbol, TDfaState> grammar,
        IReadOnlyCollection<TSymbol> nullableSymbols,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFirst)
        where TSymbol : IEquatable<TSymbol>
    {
        throw new NotImplementedException();
    }
}
