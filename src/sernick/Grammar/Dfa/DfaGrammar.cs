
namespace sernick.Grammar.Dfa;
using Common.Dfa;
using Common.Regex;
using Syntax;

/// <summary>
/// Convenience grammar class, in which right-hand sides of productions are DFAs
/// </summary>
public sealed record DfaGrammar<TSymbol>(
    TSymbol Start,
    IReadOnlyDictionary<TSymbol, SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>> Productions)
    where TSymbol : IEquatable<TSymbol>;
