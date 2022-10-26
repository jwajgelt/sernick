namespace sernick.Parser;
using sernick.Diagnostics;
using sernick.Grammar.Syntax;
using sernick.Parser.ParseTree;

public class Parser<TSymbol, TDfaState> : IParser<TSymbol, TDfaState> where TSymbol : IEquatable<TSymbol> where TDfaState : IEquatable<TDfaState>
{
    public Parser(Grammar<TSymbol> grammar)
    {
        throw new NotImplementedException();
        // throw new NotSLRGrammarException();
    }

    public IParseTree<TSymbol> process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics)
    {
        throw new NotImplementedException();
    }
}
