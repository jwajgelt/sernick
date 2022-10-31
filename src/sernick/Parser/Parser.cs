namespace sernick.Parser;

using Common.Dfa;
using Diagnostics;
using Grammar.Dfa;
using Grammar.Syntax;
using ParseTree;

#pragma warning disable CS0649
#pragma warning disable IDE0051
#pragma warning disable IDE0052
#pragma warning disable IDE0060

public sealed class Parser<TSymbol, TDfaState> : IParser<TSymbol>
    where TSymbol : class, IEquatable<TSymbol>
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
        IReadOnlyDictionary<Production<TSymbol>, IDfa<TDfaState, TSymbol>> reversedAutomata)
    {
        _reversedAutomata = reversedAutomata;
        throw new NotImplementedException();
        // throw new NotSLRGrammarException("reason");
    }

    public IParseTree<TSymbol> Process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics)
    {
        var configStack = new Stack<Configuration<TDfaState>>(new[] { _startConfig });
        var symbolStack = new Stack<TSymbol>();
        var treeStack = new Stack<IParseTree<TSymbol>>();

        void Shift(IParseTree<TSymbol> tree, Configuration<TDfaState> configuration)
        {
            treeStack.Push(tree);
            symbolStack.Push(tree.Symbol);
            configStack.Push(configuration);
        }

        using var leavesEnumerator = leaves.GetEnumerator();
        leavesEnumerator.MoveNext();
        var lookAhead = leavesEnumerator.Current;

        while (true)
        {
            var configuration = configStack.Peek();
            var parseAction = _actionTable[(configuration, lookAhead?.Symbol)];
            switch (parseAction)
            {
                case ParseActionShift<TDfaState> shiftAction:
                    Shift(lookAhead!, shiftAction.Target);

                    leavesEnumerator.MoveNext();
                    lookAhead = leavesEnumerator.Current;

                    break;
                case ParseActionReduce<TSymbol> reduceAction:
                    if (!MatchTail(
                            dfa: _reversedAutomata[reduceAction.Production],
                            symbol: reduceAction.Production.Left,
                            ref symbolStack, ref configStack, ref treeStack,
                            out var children,
                            out var nextConfig))
                    {
                        // TODO: report syntax error
                    }

                    Shift(
                        new ParseTreeNode<TSymbol>(
                            Symbol: reduceAction.Production.Left,
                            Start: children.First().Start,
                            End: children.Last().End,
                            Production: reduceAction.Production,
                            Children: children),
                        nextConfig!);

                    break;
            }
        }
    }

    private readonly Configuration<TDfaState> _startConfig;
    private readonly IReadOnlyDictionary<ValueTuple<Configuration<TDfaState>, TSymbol?>, IParseAction> _actionTable;
    private readonly IReadOnlyDictionary<Production<TSymbol>, IDfa<TDfaState, TSymbol>> _reversedAutomata;

    /// <summary>
    /// Helper method, which matches the tail of <paramref name="symbolStack"/> against <paramref name="dfa"/>.
    /// It pops from all stacks 1 element by 1, as it goes.
    /// </summary>
    /// <param name="matchedTrees">Matched tail of the <paramref name="treeStack"/> (if the method returns true)</param>
    /// <param name="nextConfig">Target configuration of the Shift action found (if the method returns true)</param>
    /// <returns>
    /// <c>true</c> if DFA reached an accepting state, and
    /// a Shift action is possible from the config from top of the stack and <paramref name="symbol"/>; <c>false</c> otherwise
    /// </returns>
    private bool MatchTail<TTree>(
        IDfa<TDfaState, TSymbol> dfa,
        TSymbol symbol,
        ref Stack<TSymbol> symbolStack,
        ref Stack<Configuration<TDfaState>> configStack,
        ref Stack<TTree> treeStack,
        out List<TTree> matchedTrees,
        out Configuration<TDfaState>? nextConfig)
    {
        matchedTrees = new List<TTree>();

        var dfaState = dfa.Start;
        while (symbolStack.Count > 0)
        {
            dfaState = dfa.Transition(dfaState, symbolStack.Pop());

            configStack.Pop();
            matchedTrees.Insert(0, treeStack.Pop());

            if (
                dfa.Accepts(dfaState) &&
                _actionTable.TryGetValue((configStack.Peek(), symbol), out var action) &&
                action is ParseActionShift<TDfaState> shiftAction)
            {
                nextConfig = shiftAction.Target;
                return true;
            }

            if (dfa.IsDead(dfaState))
            {
                nextConfig = default;
                return false;
            }
        }

        nextConfig = default;
        return false;
    }
}
