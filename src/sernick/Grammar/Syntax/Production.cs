namespace sernick.Grammar.Syntax;

using Common.Regex;

/// <summary>
/// Left -> Right (production in grammar).
/// </summary>
/// <typeparam name="TSymbol">Grammar alphabet type</typeparam>
public record Production<TSymbol>(
    TSymbol Left,
    Regex/*<TSymbol>*/ Right);
