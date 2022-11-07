namespace sernick.Parser.ParseTree;

using Input;

public interface IParseTree<TSymbol> : IEquatable<IParseTree<TSymbol>>
{
    TSymbol Symbol { get; }
    ILocation Start { get; }
    ILocation End { get; }
}
