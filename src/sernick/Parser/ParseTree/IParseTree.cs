namespace sernick.Parser.ParseTree;

using Input;

public interface IParseTree<out TSymbol>
{
    TSymbol Symbol { get; }
    ILocation Start { get; }
    ILocation End { get; }
}
