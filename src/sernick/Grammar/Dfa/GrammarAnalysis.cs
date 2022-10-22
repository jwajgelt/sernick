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
        var symbols = grammar.Productions.Select(kv => kv.Key).ToHashSet();

        // begin with FIRST(A) := {A}
        var result = grammar.Productions.ToDictionary(
            kv => kv.Key,
            kv => new HashSet<TSymbol> { kv.Key }
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
                    var next = dfa.Transition(state, nullableSymbol);
                    if (!reachable.Contains(next))
                    {
                        reachable.Add(next);
                        processing.Add(next);
                    }
                }

                processing.Remove(state);
            }

            var symbolsToAdd = reachable
                .SelectMany(state => dfa.GetTransitionsFrom(state))
                .Select(transition => transition.Atom);

            result[symbol].UnionWith(symbolsToAdd);
            // add the symbols that only show up on the right side of productions
            symbols.UnionWith(symbolsToAdd);
        }

        foreach (var symbol in symbols)
        {
            result.TryAdd(symbol, new HashSet<TSymbol> { symbol });
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

        return result.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyCollection<TSymbol>)kv.Value
        );
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

        /*
         *  The main idea of the algorithm is to utilize following relationships until a fixed point is reached:
         *  1. For a production X -> regex and for any symbol Y that can be at the end of a word in L(regex)
         *      we have that FOLLOW(X) is a subset of FOLLOW(Y), because S ->* XZ -> aYZ
         *  2. For a production X -> regex and any any word aXYb in L(regex)
         *      we have that FIRST(Y) is a subset of FOLLOW(X)
         *
         *  To make it work with our approach (using dfas on the right-hand side of the productions), we also define
         *  FOLLOW sets for dfa states. For an accepting state the FOLLOW set consists of the symbols that are present
         *  in the FOLLOW set of the symbol on the left-hand side of the production. Then we propagate these sets by
         *  visiting every edge e:
         *  1. If the symbol of e is nullable then we copy the FOLLOW set of the destination to the source.
         *  2. We copy all the elements in the FIRST set of the e symbol to the source's FOLLOW set.
         *
         *  Then the FOLLOW set of a symbol is a sum of the FOLLOW sets of all the states that can be entered with this symbol.
         */

        var followSetMap = new Dictionary<TSymbol, HashSet<TSymbol>>();
        var productionFollowSetMap = new Dictionary<TSymbol, Dictionary<TDfaState, HashSet<TSymbol>>>();

        // iterate until a fixed point is reached
        bool hasChanged;
        do
        {
            hasChanged = false;
            foreach (var production in grammar.Productions)
            {
                // the FOLLOW set of the left-hand side symbol of a production
                var keyFollowSet = followSetMap.GetOrAddSet(production.Key);
                // use queue and set to traverse from accepting states backwards using bfs
                var queue = new Queue<TDfaState>();
                var visitedStates = new HashSet<TDfaState>();

                productionFollowSetMap.TryAdd(production.Key, new Dictionary<TDfaState, HashSet<TSymbol>>());
                // the map of the FOLLOW sets for the states of this dfa
                var stateFollowSetMap = productionFollowSetMap[production.Key];

                foreach (var state in production.Value.AcceptingStates)
                {
                    queue.Enqueue(state);
                    // add the FOLLOW set of the left side of the production to the FOLLOW sets of accepting states
                    stateFollowSetMap.GetOrAddSet(state).UnionWithCheck(keyFollowSet, ref hasChanged);
                }

                while (queue.Count != 0)
                {
                    var state = queue.Dequeue();
                    visitedStates.Add(state);
                    foreach (var edge in production.Value.GetTransitionsTo(state))
                    {
                        if (!visitedStates.Contains(edge.From))
                        {
                            queue.Enqueue(edge.From);
                        }

                        // copy all the elements from the FOLLOW set of the current state to the FOLLOW set of edge.Atom
                        followSetMap.GetOrAddSet(edge.Atom).UnionWithCheck(stateFollowSetMap.GetOrAddSet(state), ref hasChanged);

                        // copy all the elements from the FIRST set of edge.Atom to the edge.From FOLLOW set
                        stateFollowSetMap.GetOrAddSet(edge.From).UnionWithCheck(symbolsFirst[edge.Atom], ref hasChanged);

                        // if edge.Atom is nullable then copy all the elements from the FOLLOW set of the current state to edge.FROM
                        if (nullableSymbols.Contains(edge.Atom))
                        {
                            stateFollowSetMap.GetOrAddSet(edge.From).UnionWithCheck(stateFollowSetMap.GetOrAddSet(state), ref hasChanged);
                        }
                    }
                }
            }
        } while (hasChanged);

        var result = new Dictionary<TSymbol, IReadOnlyCollection<TSymbol>>();
        foreach (var (key, value) in followSetMap)
        {
            result[key] = value;
        }

        return result;
    }
}

internal static class GrammarAnalysisHelpers
{
    public static HashSet<TValue> GetOrAddSet<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> dictionary, TKey key)
    {
        dictionary.TryAdd(key, new HashSet<TValue>());
        return dictionary[key];
    }

    public static void UnionWithCheck<T>(this HashSet<T> set, IEnumerable<T> other, ref bool hasChanged)
    {
        var count = set.Count;
        set.UnionWith(other);
        hasChanged = hasChanged || count != set.Count;
    }
}
