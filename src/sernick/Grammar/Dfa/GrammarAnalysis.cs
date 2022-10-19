namespace sernick.Grammar.Dfa;

public static class GrammarAnalysis
{
    /// <summary>
    /// Compute the set NULLABLE - all symbols, from which epsilon can be derived in grammar
    /// </summary>
    public static IReadOnlyCollection<TSymbol> Nullable<TSymbol, TDfaState>(
        this DfaGrammar<TSymbol, TDfaState> grammar)
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
    {
        throw new NotImplementedException();
    }
}
