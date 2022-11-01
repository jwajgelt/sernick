namespace sernick.Parser;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Common.Dfa;
using Diagnostics;
using Grammar.Dfa;
using Grammar.Syntax;
using ParseTree;

#pragma warning disable CS0649
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
        DfaGrammar<TDfaState> dfaGrammar,
        IReadOnlyCollection<TSymbol> symbolsNullable,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFirst,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFollow,
        IReadOnlyDictionary<Production<TSymbol>, IDfa<TDfaState, TSymbol>> reversedAutomata)
    {
        _startSymbol = dfaGrammar.Start;
        _reversedAutomata = reversedAutomata;
        throw new NotImplementedException();
        // throw new NotSLRGrammarException("reason");
    }

    public IParseTree<TSymbol> Process(IEnumerable<IParseTree<TSymbol>> leaves, IDiagnostics diagnostics)
    {
        var state = new State(_startConfig);

        using var leavesEnumerator = leaves.GetEnumerator();
        leavesEnumerator.MoveNext();
        var lookAhead = leavesEnumerator.Current;

        while (true)
        {
            var parseAction = _actionTable[(state.Configuration, lookAhead?.Symbol)];
            switch (parseAction)
            {
                case null:
                    if (lookAhead is null && state.TreeStack.Count == 1 && state.Tree.Symbol.Equals(_startSymbol))
                    {
                        return state.TreeStack.Single();
                    }

                    diagnostics.Report(new SyntaxError<TSymbol>(lookAhead));
                    throw new ParsingException("No parsing action available at current state");

                case ParseActionShift<TDfaState> shiftAction:
                    Debug.Assert(lookAhead is not null, $"actionTable[(config, {null})] mustn't be Shift");

                    state.Push(shiftAction.Target, lookAhead);

                    leavesEnumerator.MoveNext();
                    lookAhead = leavesEnumerator.Current;

                    break;

                case ParseActionReduce<TSymbol> reduceAction:
                    if (!MatchTail(
                            dfa: _reversedAutomata[reduceAction.Production],
                            symbol: reduceAction.Production.Left,
                            state,
                            out var children,
                            out var nextConfig))
                    {
                        diagnostics.Report(new SyntaxError<TSymbol>(state.TreeStack.FirstOrDefault()));
                        throw new ParsingException("The subsequence of tokens cannot be parsed");
                    }

                    state.Push(nextConfig,
                        new ParseTreeNode<TSymbol>(
                            Symbol: reduceAction.Production.Left,
                            Start: children.First().Start,
                            End: children.Last().End,
                            Production: reduceAction.Production,
                            Children: children));

                    break;
            }
        }
    }

    private readonly TSymbol _startSymbol;
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
    private bool MatchTail(
        IDfa<TDfaState, TSymbol> dfa,
        TSymbol symbol,
        State state,
        out List<IParseTree<TSymbol>> matchedTrees,
        [NotNullWhen(true)]
        out Configuration<TDfaState>? nextConfig)
    {
        matchedTrees = new List<IParseTree<TSymbol>>();

        var dfaState = dfa.Start;
        while (state.TreeStack.Count > 0)
        {
            var tree = state.Pop();

            dfaState = dfa.Transition(dfaState, tree.Symbol);

            matchedTrees.Insert(0, tree);

            if (
                dfa.Accepts(dfaState) &&
                _actionTable.TryGetValue((state.Configuration, symbol), out var action) &&
                action is ParseActionShift<TDfaState> shiftAction)
            {
                nextConfig = shiftAction.Target;
                return true;
            }

            if (dfa.IsDead(dfaState))
            {
                break;
            }
        }

        nextConfig = default;
        return false;
    }

    private sealed class State
    {
        internal State(Configuration<TDfaState> startConfig) => ConfigStack.Push(startConfig);

        private Stack<Configuration<TDfaState>> ConfigStack { get; } = new();
        internal Stack<IParseTree<TSymbol>> TreeStack { get; } = new();

        internal Configuration<TDfaState> Configuration => ConfigStack.Peek();
        internal IParseTree<TSymbol> Tree => TreeStack.Peek();

        internal void Push(Configuration<TDfaState> configuration, IParseTree<TSymbol> tree)
        {
            ConfigStack.Push(configuration);
            TreeStack.Push(tree);
        }

        internal IParseTree<TSymbol> Pop()
        {
            ConfigStack.Pop();
            return TreeStack.Pop();
        }
    }
}

public sealed class ParsingException : Exception
{
    public ParsingException(string message) : base(message) { }
}