namespace sernick.Grammar.Dfa;

using Common.Dfa;

/// <summary>
/// Convenience grammar class, in which right-hand sides of productions are DFAs
/// </summary>
public sealed record DfaGrammar<TLabel, TDfaState, TSymbol>(
    TSymbol Start,
    IReadOnlyDictionary<TSymbol, SumDfa<TLabel, TDfaState, TSymbol>> Productions)
    where TSymbol : IEquatable<TSymbol> where TLabel : notnull;
