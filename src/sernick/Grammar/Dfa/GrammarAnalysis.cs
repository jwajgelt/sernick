namespace sernick.Grammar.Dfa;

public static class GrammarAnalysis
{

    /// <summary>
    /// Compute the set NULLABLE - all symbols, from which epsilon can be derived in grammar
    /// </summary>
    public static IReadOnlyCollection<TSymbol> Nullable<TSymbol, TDfaState>(
        this DfaGrammar<TSymbol, TDfaState> grammar) where TSymbol : IEquatable<TSymbol> where TDfaState : IEquatable<TDfaState>
    {
        var Nullable = new List<TSymbol>();
        // Use Tuple, so we remember which automata does every state come from
        var Q = new Queue<Tuple<TSymbol, TDfaState>>();
        var ConditionalQueues = new Dictionary<TSymbol, Queue<TDfaState>>();

        foreach (var symbol in grammar.Productions.Keys)
        {
            ConditionalQueues.Add(symbol, new Queue<TDfaState>());
        }

        foreach (var symbol in grammar.Productions.Keys)
        {
            foreach (var acceptingState in grammar.Productions[symbol].AcceptingStates)
            {
                Q.Enqueue(Tuple.Create(symbol, acceptingState));
            }
        }

        while (Q.Count != 0)
        {
            var tuple = Q.Dequeue();
            var symbolFromGrammar = tuple.Item1;
            var state = tuple.Item2;

            var currentAutomata = grammar.Productions[symbolFromGrammar];

            // If we've encountered a start symbol for DFA => add all states from "conditional set" for "symbol" to Q
            // and mark current symbol as nullable
            if (Equals(state, currentAutomata.Start))
            {
                foreach (var stateForSymbol in ConditionalQueues[symbolFromGrammar])
                {
                    Q.Enqueue(Tuple.Create(symbolFromGrammar, stateForSymbol));
                }

                Nullable.Append(symbolFromGrammar);
                ConditionalQueues[symbolFromGrammar].Clear();
            }

            try
            {
                foreach (var transitionEdge in grammar.Productions[symbolFromGrammar].GetTransitionsTo(state))
                {
                    var fromState = transitionEdge.From;
                    var atom = transitionEdge.Atom;
                    if (Nullable.Contains(atom))
                    {
                        Q.Enqueue(Tuple.Create(symbolFromGrammar, fromState));
                    }
                    else
                    {
                        ConditionalQueues[atom].Enqueue(fromState);
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                // our automata throws exception, instead of returning an empty list, when no transitions
                // ignore and continue
            }
        }

        return Nullable;
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
        throw new NotImplementedException();
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
