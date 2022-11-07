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
    public override int GetHashCode() => (Symbol, Production, Children).GetHashCode();
}
