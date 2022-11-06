namespace sernick.Parser.ParseTree;

using Grammar.Lexicon;
using Input;

public sealed record ParseTreeLeaf<TSymbol>(
    TSymbol Symbol,
    ILocation Start,
    ILocation End
) : IParseTree<TSymbol>,
    IEquatable<IParseTree<TSymbol>>
    where TSymbol : ILexicalGrammarCategory
{
    public bool Equals(ParseTreeLeaf<TSymbol>? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Symbol is not IEquatable<TSymbol> equatable)
        {
            return false;
        }

        return equatable.Equals(other!.Symbol);
    }

    public bool Equals(IParseTree<TSymbol>? other)
    {
        if (other is ParseTreeLeaf<TSymbol> otherLeaf)
        {
            return Equals(otherLeaf);
        }

        return false;
    }
}
