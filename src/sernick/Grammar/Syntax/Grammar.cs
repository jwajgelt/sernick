namespace sernick.Grammar.Syntax;

/// <summary>
/// Syntactical Grammar.
/// </summary>
/// <param name="Start">Starting symbol in all derivations</param>
/// <param name="Productions">List of possible productions</param>
/// <typeparam name="TSymbol">Grammar alphabet type</typeparam>
public sealed record Grammar<TSymbol>(
    TSymbol Start,
    IEnumerable<Production<TSymbol>> Productions);
