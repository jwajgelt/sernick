namespace sernick.Grammar.Dfa;

public static class GrammarAnalysis<TSymbol> where TSymbol : notnull
{
    /// <summary>
    /// Compute the set NULLABLE - all symbols, from which epsilon can be derived in grammar
    /// </summary>
    public static IReadOnlyCollection<TSymbol> Nullable<TDfaState>(
        this DfaGrammar<TSymbol, TDfaState> grammar)
        where TSymbol : IEquatable<TSymbol>
    {
        List<TSymbol> Nullable = new List<TSymbol>();
        // Use Tuple, so we remember which automata does every state come from
        Queue<Tuple<TSymbol, TDfaState>> Q = new Queue<Tuple<TSymbol, TDfaState>>();
        Dictionary<TSymbol, Queue<TDfaState>> ConditionalQueues = new Dictionary<TSymbol, Queue<TDfaState>>();

        foreach (var symbol in grammar.Productions.Keys)
        {
            foreach (var acceptingState in grammar.Productions[symbol].AcceptingStates)
            {
                Q.Append(Tuple.Create(symbol, acceptingState));
            }
        }

        while (Q.Count != 0)
        {
            Tuple<TSymbol, TDfaState> tuple = Q.Dequeue();
            TSymbol symbol = tuple.Item1;
            TDfaState state = tuple.Item2;
            foreach (var transitionEdge in grammar.Productions[symbol].GetTransitionsTo(state))
            {
                var fromState = transitionEdge.From;
                if (Nullable.Contains(transitionEdge.Atom))
                {
                    Nullable.Add(transitionEdge.Atom);
                }

            }

        }


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
