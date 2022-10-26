namespace sernick.Parser.ParseTree;

using sernick.Grammar.Lexicon;
using sernick.Input;

public class ParseTreeLeaf<TSymbol> : IParseTree<TSymbol>
    where TSymbol : ILexicalGrammarCategory
{
    public ParseTreeLeaf(
        TSymbol symbol,
        ILocation start,
        ILocation end
    ) => (Symbol, Start, End) = (symbol, start, end);
    
    public TSymbol Symbol { get; }
    public ILocation Start { get; }
    public ILocation End { get; }
}
