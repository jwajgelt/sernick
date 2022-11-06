namespace sernick.Parser;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Common.Dfa;
using Common.Regex;
using Diagnostics;
using Grammar.Dfa;
using Grammar.Syntax;
using ParseTree;
using Utility;

public sealed class Parser<TSymbol> : IParser<TSymbol>
    where TSymbol : class, IEquatable<TSymbol>
{
    private readonly TSymbol _startSymbol;
    private readonly Configuration<TSymbol> _startConfig;
    private readonly IReadOnlyDictionary<ValueTuple<Configuration<TSymbol>, TSymbol?>, IParseAction> _actionTable;
    private readonly IReadOnlyDictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>> _reversedAutomata;

    public static Parser<TSymbol> FromGrammar(Grammar<TSymbol> grammar)
    {
        var dfaGrammar = grammar.ToDfaGrammar();
        var nullable = dfaGrammar.Nullable();
        var first = dfaGrammar.First(nullable);
        var follow = dfaGrammar.Follow(nullable, first);
        var reversedAutomatas = dfaGrammar.GetReverseAutomatas();

        return new Parser<TSymbol>(dfaGrammar,
            follow,
            reversedAutomatas);
    }

    internal Parser(
        DfaGrammar<TSymbol> dfaGrammar,
        IReadOnlyDictionary<TSymbol, IReadOnlyCollection<TSymbol>> symbolsFollow,
        IReadOnlyDictionary<Production<TSymbol>, IDfa<Regex<TSymbol>, TSymbol>> reversedAutomata)
    {
        _startSymbol = dfaGrammar.Start;
        _reversedAutomata = reversedAutomata;
        _startConfig = new Configuration<TSymbol>(dfaGrammar.Productions
            .Select(production => (production.Value.Start, production.Key)).ToHashSet());
        var actionTable = new Dictionary<ValueTuple<Configuration<TSymbol>, TSymbol?>, IParseAction>();

        // traverse all reachable configurations using bfs
        var queue = new Queue<Configuration<TSymbol>>();
        var visitedConfigs = new HashSet<Configuration<TSymbol>>();

        queue.Enqueue(_startConfig);
        visitedConfigs.Add(_startConfig);

        while (queue.Count > 0)
        {
            var currentConfig = queue.Dequeue();

            // collect all outgoing edges for each state in the current configuration
            var symbolToStatesMap =
                new Dictionary<TSymbol,
                    HashSet<(SumDfa<Production<TSymbol>, Regex<TSymbol>, TSymbol>.State state, TSymbol symbol)>>();
            foreach (var (state, symbol) in currentConfig.States)
            {
                foreach (var edge in dfaGrammar.Productions[symbol].GetTransitionsFrom(state))
                {
                    symbolToStatesMap.GetOrAddEmpty(edge.Atom).Add((edge.To, symbol));
                }
            }

            // calculate possible shifts from the current state
            foreach (var (symbol, states) in symbolToStatesMap)
            {
                var nextConfig = Configuration<TSymbol>.Closure(
                    states.Where(item => !dfaGrammar.Productions[item.symbol].IsDead(item.state)),
                    dfaGrammar
                ); // closure of reachable not-dead states

                if (nextConfig.States.Count == 0)
                {
                    continue;
                }

                actionTable[(currentConfig, symbol)] = new ParseActionShift<TSymbol>(nextConfig);

                if (!visitedConfigs.Contains(nextConfig))
                {
                    queue.Enqueue(nextConfig);
                    visitedConfigs.Add(nextConfig);
                }
            }

            // calculate possible reductions from the current state
            foreach (var (state, symbol) in currentConfig.States)
            {
                foreach (var production in dfaGrammar.Productions[symbol].AcceptingCategories(state))
                {
                    // SLR only allows for a reduction A->E, when the next symbol is in the follow set of A or we reached the end
                    foreach (var followingSymbol in symbolsFollow[symbol].Append(null))
                    {
                        if (actionTable.TryAdd((currentConfig, followingSymbol),
                                new ParseActionReduce<TSymbol>(production)))
                        {
                            continue;
                        }

                        switch (actionTable[(currentConfig, followingSymbol)])
                        {
                            case ParseActionShift<TSymbol>:
                                throw NotSLRGrammarException.ShiftReduceConflict(followingSymbol, production);
                            case ParseActionReduce<TSymbol> reduce:
                                throw NotSLRGrammarException.ReduceReduceConflict(symbol, production, reduce.Production);
                        }
                    }
                }
            }
        }

        _actionTable = actionTable;
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

                case ParseActionShift<TSymbol> shiftAction:
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
        IDfa<Regex<TSymbol>, TSymbol> dfa,
        TSymbol symbol,
        State state,
        out List<IParseTree<TSymbol>> matchedTrees,
        [NotNullWhen(true)]
        out Configuration<TSymbol>? nextConfig)
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
                action is ParseActionShift<TSymbol> shiftAction)
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
        internal State(Configuration<TSymbol> startConfig) => ConfigStack.Push(startConfig);

        private Stack<Configuration<TSymbol>> ConfigStack { get; } = new();
        internal Stack<IParseTree<TSymbol>> TreeStack { get; } = new();

        internal Configuration<TSymbol> Configuration => ConfigStack.Peek();
        internal IParseTree<TSymbol> Tree => TreeStack.Peek();

        internal void Push(Configuration<TSymbol> configuration, IParseTree<TSymbol> tree)
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
