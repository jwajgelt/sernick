namespace sernick.Parser.ParseTree;

using sernick.Input;

public interface IParseTree<TSymbol>
{
    TSymbol Symbol { get; }
    ILocation Start { get; }
    ILocation End { get; }
}
