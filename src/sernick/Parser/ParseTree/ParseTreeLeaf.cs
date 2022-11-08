namespace sernick.Parser.ParseTree;

using Input;

public sealed record ParseTreeLeaf<TSymbol>(
    TSymbol Symbol,
    ILocation Start,
    ILocation End
) : IParseTree<TSymbol>
    where TSymbol : class, IEquatable<TSymbol>
{
    public bool Equals(ParseTreeLeaf<TSymbol>? other) => Symbol.Equals(other?.Symbol);
    public bool Equals(IParseTree<TSymbol>? other) => Equals(other as ParseTreeLeaf<TSymbol>);
    public override int GetHashCode() => Symbol.GetHashCode();
}
