namespace sernick.Parser.ParseTree;

using sernick.Grammar.Syntax;
using sernick.Input;

public class ParseTreeNode<TSymbol> : IParseTree<TSymbol>
    where TSymbol : IEquatable<TSymbol>
{
    public ParseTreeNode(
        TSymbol symbol,
        ILocation start,
        ILocation end,
        Production<TSymbol> production,
        IEnumerable<IParseTree<TSymbol>> children
    ) => (Symbol, Start, End, Production, Children) = (symbol, start, end, production, children);

    public TSymbol Symbol { get; }
    public ILocation Start { get; }
    public ILocation End { get; }
    public Production<TSymbol> Production { get; }
    public IEnumerable<IParseTree<TSymbol>> Children { get; }
}
