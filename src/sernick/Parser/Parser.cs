using sernick.Common.Regex;

namespace sernick.Parser;

using Common.Dfa;
using Diagnostics;
using Grammar.Dfa;
using Grammar.Syntax;
using ParseTree;

#pragma warning disable IDE0051
#pragma warning disable IDE0052
#pragma warning disable IDE0060

public sealed class Parser<TSymbol, TDfaState> : IParser<TSymbol>
    where TSymbol : class, IEquatable<TSymbol>
    where TDfaState : IEquatable<TDfaState>
{
    private readonly Configuration<TDfaState> _startConfig;
    private readonly IReadOnlyDictionary<ValueTuple<Configuration<TDfaState>, TSymbol?>, IParseAction> _actionTable;
    private readonly IReadOnlyDictionary<Production<TSymbol>, IDfa<TDfaState, TSymbol>> _reversedAutomata;

    public static Parser<TSymbol, Regex<TSymbol>> FromGrammar(Grammar<TSymbol> grammar)
    {
        var dfaGrammar = grammar.ToDfaGrammar();
        var nullable = dfaGrammar.Nullable();
        var first = dfaGrammar.First(nullable);
        var follow = dfaGrammar.Follow(nullable, first);
        var reversedAutomatas = GetReverseAutomatas(dfaGrammar);

        return new Parser<TSymbol, Regex<TSymbol>>(dfaGrammar,
            nullable,
            first,
            follow,
            reversedAutomatas);
    }

    internal Parser(
        DfaGrammar<TSymbol, TDfaState> dfaGrammar,
        IReadOnlyCollection<TSymbol> symbolsNullable,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFirst,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFollow,
        IReadOnlyDictionary<Production<TSymbol>, IDfa<TDfaState, TSymbol>> reversedAutomata)
    {
        _reversedAutomata = reversedAutomata;
        throw new NotImplementedException();
        // throw new NotSLRGrammarException("reason");
    }

    public IParseTree<TSymbol> Process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics)
    {
        throw new NotImplementedException();
    }

    private static Dictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>> GetReverseAutomatas(DfaGrammar<TSymbol, Regex<TSymbol>> dfaGrammar)
    {
        var reversedAutomatas = new Dictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>>();
        foreach (var production in dfaGrammar.Productions)
        {
            var symbol = production.Key;
            var dfa = (RegexDfa<TSymbol>)production.Value;
            reversedAutomatas[new Production<TSymbol>(symbol, dfa.Start)] = dfa.Reverse();
        }

        return reversedAutomatas;
    }
}
