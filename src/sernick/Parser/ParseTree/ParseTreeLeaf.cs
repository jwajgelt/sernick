namespace sernick.Parser.ParseTree;

using Input;
using Utility;

public sealed record ParseTreeLeaf<TSymbol>(
    TSymbol Symbol,
    Range<ILocation> LocationRange
) : IParseTree<TSymbol>
    where TSymbol : class, IEquatable<TSymbol>
{
    public bool Equals(ParseTreeLeaf<TSymbol>? other) => Symbol.Equals(other?.Symbol);
    public bool Equals(IParseTree<TSymbol>? other) => Equals(other as ParseTreeLeaf<TSymbol>);
    public override int GetHashCode() => Symbol.GetHashCode();
}
