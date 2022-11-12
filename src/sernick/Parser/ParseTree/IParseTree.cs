namespace sernick.Parser.ParseTree;

using Input;
using Utility;

public interface IParseTree<TSymbol> : IEquatable<IParseTree<TSymbol>>
{
    TSymbol Symbol { get; }
    Range<ILocation> LocationRange { get; }
}
