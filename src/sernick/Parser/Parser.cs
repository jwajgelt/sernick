namespace sernick.Parser;

using sernick.Common.Dfa;
using sernick.Diagnostics;
using sernick.Grammar.Dfa;
using sernick.Grammar.Syntax;
using sernick.Parser.ParseTree;

#pragma warning disable IDE0051

public class Parser<TSymbol, TDfaState> : IParser<TSymbol, TDfaState>
    where TSymbol : IEquatable<TSymbol>
    where TDfaState : IEquatable<TDfaState>
{
    public static Parser<TSymbol, TDfaState> FromGrammar(Grammar<TSymbol> grammar)
    {
        throw new NotImplementedException();
    }

    private Parser(
        DfaGrammar<TSymbol, TDfaState> dfaGrammar,
        IReadOnlyCollection<TSymbol> symbolsNullable,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFirst,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFollow,
        IReadOnlyDictionary<Production<TSymbol>, IDfaWithConfig<TSymbol, TDfaState>> reversedAutomata)
    {
        throw new NotImplementedException();
        // throw new NotSLRGrammarException();
    }

    public IParseTree<TSymbol> process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics)
    {
        throw new NotImplementedException();
    }

    private IReadOnlyDictionary<ValueTuple<Configuration<TDfaState>, TSymbol>, IParseAction> ActionTable { get; set; }

    private Configuration<TDfaState> StartConfig { get; set; }
}
