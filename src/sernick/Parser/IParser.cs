namespace sernick.Parser;

using sernick.Diagnostics;
using sernick.Parser.ParseTree;

public interface IParser<TSymbol, TDfaState>
    where TSymbol : IEquatable<TSymbol>
{
    IParseTree<TSymbol> process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics);
}
