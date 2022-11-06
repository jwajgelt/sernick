namespace sernick.Parser.ParseTree;

using Grammar.Lexicon;
using Input;

public sealed record ParseTreeLeaf<TSymbol>(
    TSymbol Symbol,
    ILocation Start,
    ILocation End
) : IParseTree<TSymbol>
    where TSymbol : ILexicalGrammarCategory, IEquatable<TSymbol>
{
    public bool Equals(ParseTreeLeaf<TSymbol>? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other != null && Symbol.Equals(other.Symbol);
    }

    public bool Equals(IParseTree<TSymbol>? other) => Equals(other as ParseTreeLeaf<TSymbol>);
}
