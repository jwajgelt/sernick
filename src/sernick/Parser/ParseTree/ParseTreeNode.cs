namespace sernick.Parser.ParseTree;

using Grammar.Syntax;
using Input;
using Utility;

public sealed record ParseTreeNode<TSymbol>(
    TSymbol Symbol,
    Production<TSymbol> Production,
    IReadOnlyList<IParseTree<TSymbol>> Children,
    Range<ILocation> LocationRange
) : IParseTree<TSymbol>
    where TSymbol : IEquatable<TSymbol>
{
    public bool Equals(ParseTreeNode<TSymbol>? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null &&
               (Symbol, Production).Equals((other.Symbol, other.Production)) &&
               Children.SequenceEqual(other.Children);
    }

    public bool Equals(IParseTree<TSymbol>? other) => Equals(other as ParseTreeNode<TSymbol>);
    public override int GetHashCode() => HashCode.Combine(Symbol, Production, Children.GetCombinedHashCode());
}
