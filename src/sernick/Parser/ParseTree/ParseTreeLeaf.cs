namespace sernick.Parser.ParseTree;

using Grammar.Lexicon;
using Input;

public sealed record ParseTreeLeaf<TSymbol>(
    TSymbol Symbol,
    ILocation Start,
    ILocation End
) : IParseTree<TSymbol>
    where TSymbol : ILexicalGrammarCategory;
