using sernick.Grammar.Syntax;

namespace sernick.Grammar.Dfa;

using Common.Dfa;

/// <summary>
/// Convenience grammar class, in which right-hand sides of productions are DFAs
/// </summary>
public sealed record DfaGrammar<TDfaState, TSymbol>(
    TSymbol Start,
    IReadOnlyDictionary<TSymbol, SumDfa<Production<TSymbol>, TDfaState, TSymbol>> Productions)
    where TSymbol : IEquatable<TSymbol>;
