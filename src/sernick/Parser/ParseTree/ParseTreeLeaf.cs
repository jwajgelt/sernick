namespace sernick.Parser.ParseTree;

using sernick.Grammar.Lexicon;
using sernick.Input;

public sealed record ParseTreeLeaf<TSymbol>(
    TSymbol Symbol,
    ILocation Start,
    ILocation End
) : IParseTree<TSymbol>
    where TSymbol : ILexicalGrammarCategory;
