namespace sernick.Grammar.Dfa;

using Utility;

public static class GrammarAnalysis
{

    /// <summary>
    /// Compute the set NULLABLE - all symbols, from which epsilon can be derived in grammar
    /// </summary>
    public static IReadOnlyCollection<TSymbol> Nullable<TSymbol, TDfaState>(
        this DfaGrammar<TSymbol, TDfaState> grammar)
         where TSymbol : IEquatable<TSymbol>
         where TDfaState : IEquatable<TDfaState>
    {
        var nullable = new List<TSymbol>();
        // Use ValueTuple here and below, so we remember which automata does every state come from
        var queue = new Queue<ValueTuple<TSymbol, TDfaState>>();
        var conditionalQueuesForSymbols = grammar.Productions.Keys.ToDictionary(symbol => symbol, _ => new Queue<ValueTuple<TSymbol, TDfaState>>());

        foreach (var symbol in grammar.Productions.Keys)
        {
            foreach (var acceptingState in grammar.Productions[symbol].AcceptingStates)
            {
                queue.Enqueue((symbol, acceptingState));
            }
        }

        while (queue.Count != 0)
        {
            var (currentSymbolWhichDeterminesAutomata, currentState) = queue.Dequeue();

            var currentAutomata = grammar.Productions[currentSymbolWhichDeterminesAutomata];

            // If we've encountered a start symbol for DFA => add all states from "conditional set" for "symbol" to Q
            // and mark current symbol as nullable
            if (currentAutomata.Start.Equals(currentState))
            {
                foreach (var (symbolWhichDeterminesAutomata, stateForThatSymbol) in conditionalQueuesForSymbols[currentSymbolWhichDeterminesAutomata])
                {

                    queue.Enqueue((symbolWhichDeterminesAutomata, stateForThatSymbol));
                }

                nullable.Add(currentSymbolWhichDeterminesAutomata);
                conditionalQueuesForSymbols[currentSymbolWhichDeterminesAutomata].Clear();
            }

            foreach (var transitionEdge in grammar.Productions[currentSymbolWhichDeterminesAutomata].GetTransitionsTo(currentState))
            {
                var fromState = transitionEdge.From;
                var atom = transitionEdge.Atom;
                if (nullable.Contains(atom))
                {
                    queue.Enqueue((currentSymbolWhichDeterminesAutomata, fromState));
                }
                else
                {
                    // we need to remember that "fromState" is a state for a specific automata (one determined by symbolFromGrammar)
                    conditionalQueuesForSymbols[atom].Enqueue((currentSymbolWhichDeterminesAutomata, fromState));
                }
            }
        }

        return nullable;
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
        where TDfaState : notnull
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
                var keyFollowSet = followSetMap.GetOrAddEmpty(production.Key);
                // use queue and set to traverse from accepting states backwards using bfs
                var queue = new Queue<TDfaState>();
                var visitedStates = new HashSet<TDfaState>();

                // the map of the FOLLOW sets for the states of this dfa
                var stateFollowSetMap = productionFollowSetMap.GetOrAddEmpty(production.Key);

                foreach (var state in production.Value.AcceptingStates)
                {
                    queue.Enqueue(state);
                    // add the FOLLOW set of the left side of the production to the FOLLOW sets of accepting states
                    hasChanged |= stateFollowSetMap.GetOrAddEmpty(state).UnionWithCheck(keyFollowSet);
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
                        hasChanged |= followSetMap.GetOrAddEmpty(edge.Atom).UnionWithCheck(stateFollowSetMap.GetOrAddEmpty(state));

                        // copy all the elements from the FIRST set of edge.Atom to the edge.From FOLLOW set
                        hasChanged |= stateFollowSetMap.GetOrAddEmpty(edge.From).UnionWithCheck(symbolsFirst[edge.Atom]);

                        // if edge.Atom is nullable then copy all the elements from the FOLLOW set of the current state to edge.FROM
                        if (nullableSymbols.Contains(edge.Atom))
                        {
                            hasChanged |= stateFollowSetMap.GetOrAddEmpty(edge.From).UnionWithCheck(stateFollowSetMap.GetOrAddEmpty(state));
                        }
                    }
                }
            }
        } while (hasChanged);

        return followSetMap.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<TSymbol>)kv.Value);
    }
}

