namespace sernick.Parser.ParseTree;

using Grammar.Syntax;
using Input;
using Utility;

public sealed record ParseTreeNode<TSymbol>(
    TSymbol Symbol,
    Production<TSymbol> Production,
    IEnumerable<IParseTree<TSymbol>> Children,
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

        if (!Equals(Symbol, other!.Symbol) ||
            Children.Count() != other.Children.Count() ||
            Production != other.Production)
        {
            return false;
        }

        var zipped = Children.Zip(other.Children, (first, second) => (First: first, Second: second));
        return zipped.All(pair => Equals(pair.First, pair.Second));
    }

    public bool Equals(IParseTree<TSymbol>? other) => Equals(other as ParseTreeNode<TSymbol>);
    public override int GetHashCode()
    {
        var childrenHashCode = Children.Aggregate(
            Children.Count(),
            (hashCode, child) => unchecked(hashCode * 17 + child.GetHashCode())
        );
        return (Symbol, Production, childrenHashCode).GetHashCode();
    }
}
