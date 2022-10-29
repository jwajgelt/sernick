namespace sernick.Grammar.Dfa;

using Common.Dfa;

/// <summary>
/// Convenience grammar class, in which right-hand sides of productions are DFAs
/// </summary>
public sealed record DfaGrammar<TSymbol, TDfaState>(
    TSymbol Start,
    IReadOnlyDictionary<TSymbol, IDfa<TDfaState, TSymbol>> Productions)
    where TSymbol : IEquatable<TSymbol>;
