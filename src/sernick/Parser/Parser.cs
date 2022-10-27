namespace sernick.Parser;

using Common.Dfa;
using Diagnostics;
using Grammar.Dfa;
using Grammar.Syntax;
using ParseTree;

#pragma warning disable IDE0051

public sealed class Parser<TSymbol, TDfaState> : IParser<TSymbol>
    where TSymbol : IEquatable<TSymbol>
    where TDfaState : IEquatable<TDfaState>
{
    public static Parser<TSymbol, TDfaState> FromGrammar(Grammar<TSymbol> grammar)
    {
        throw new NotImplementedException();
    }

    internal Parser(
        DfaGrammar<TSymbol, TDfaState> dfaGrammar,
        IReadOnlyCollection<TSymbol> symbolsNullable,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFirst,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFollow,
        IReadOnlyDictionary<Production<TSymbol>, IDfaWithConfig<TSymbol, TDfaState>> reversedAutomata)
    {
        throw new NotImplementedException();
        // throw new NotSLRGrammarException("reason");
    }

    public IParseTree<TSymbol> Process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics)
    {
        throw new NotImplementedException();
    }

    private readonly IReadOnlyDictionary<ValueTuple<Configuration<TDfaState>, TSymbol>, IParseAction> _actionTable;
    private readonly Configuration<TDfaState> _startConfig;
}
