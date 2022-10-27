namespace sernick.Parser;

using Diagnostics;
using ParseTree;

public interface IParser<TSymbol>
    where TSymbol : IEquatable<TSymbol>
{
    IParseTree<TSymbol> Process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics);
}
