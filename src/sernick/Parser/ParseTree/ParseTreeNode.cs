namespace sernick.Parser.ParseTree;

using Grammar.Syntax;
using Input;

public sealed record ParseTreeNode<TSymbol>(
    TSymbol Symbol,
    ILocation Start,
    ILocation End,
    Production<TSymbol> Production,
    IEnumerable<IParseTree<TSymbol>> Children
) : IParseTree<TSymbol>
    where TSymbol : IEquatable<TSymbol>;
