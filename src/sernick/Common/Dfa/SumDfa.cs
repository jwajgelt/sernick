namespace sernick.Common.Dfa;

using System.Collections.ObjectModel;
using Utility;

public sealed class SumDfa<TCat, TState, TSymbol> : IDfa<SumDfa<TCat, TState, TSymbol>.State, TSymbol>
    where TCat : notnull
    where TSymbol : IEquatable<TSymbol>
{
    private readonly IReadOnlyDictionary<TCat, IDfa<TState, TSymbol>> _dfas;

    private Dictionary<State, List<TransitionEdge<State, TSymbol>>>?
        _transitionsToMap;

    private HashSet<State>? _acceptingStates;

    public SumDfa(IReadOnlyDictionary<TCat, IDfa<TState, TSymbol>> dfas)
    {
        _dfas = dfas;
        Start = new State(dfas.ToDictionary(kv => kv.Key, kv => kv.Value.Start));
    }

    public State Start { get; }
    public IEnumerable<TransitionEdge<State, TSymbol>> GetTransitionsFrom(State state)
    {
        return _dfas
            .SelectMany(kv => kv.Value.GetTransitionsFrom(state[kv.Key]))
            .Select(transition => transition.Atom)
            .ToHashSet()    // only process each atom once
            .Select(atom =>
                new TransitionEdge<State, TSymbol>(state, Transition(state, atom), atom)
            );
    }

    public IEnumerable<TransitionEdge<State, TSymbol>> GetTransitionsTo(State state)
    {
        if (_transitionsToMap == null)
        {
            InitializeTransitionsDictionary();
        }

        return _transitionsToMap![state];
    }

    public IEnumerable<State> AcceptingStates
    {
        get
        {
            if (_acceptingStates == null)
            {
                InitializeTransitionsDictionary();
            }

            return _acceptingStates!;
        }
    }

    public bool Accepts(State state) =>
        state.Any(kv => _dfas[kv.Key].Accepts(kv.Value));

    public bool IsDead(State state) =>
        state.All(kv => _dfas[kv.Key].IsDead(kv.Value));

    public State Transition(State state, TSymbol atom) =>
        new(state.ToDictionary(
            kv => kv.Key,
            kv => _dfas[kv.Key].Transition(kv.Value, atom)
        ));

    public IEnumerable<TCat> AcceptingCategories(State state) =>
        state.Where(kv => _dfas[kv.Key].Accepts(kv.Value)).Select(kv => kv.Key);

    private void InitializeTransitionsDictionary()
    {
        _acceptingStates = new HashSet<State>();
        var transitionsToMap =
            new Dictionary<State, List<TransitionEdge<State, TSymbol>>>();
        var visited = new HashSet<State>();
        var queue = new Queue<State>();

        queue.Enqueue(Start);
        visited.Add(Start);
        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            if (Accepts(currentState))
            {
                _acceptingStates.Add(currentState);
            }

            foreach (var transition in GetTransitionsFrom(currentState))
            {
                var nextState = transition.To;
                transitionsToMap.GetOrAddEmpty(nextState).Add(transition);

                if (visited.Contains(nextState))
                {
                    continue;
                }

                queue.Enqueue(nextState);
                visited.Add(nextState);
            }
        }

        _transitionsToMap = transitionsToMap.ToDictionary(
            kv => kv.Key,
            kv => kv.Value
            );
    }

    public sealed class State : ReadOnlyDictionary<TCat, TState>, IEquatable<State>
    {
        public State(IDictionary<TCat, TState> partStates) : base(partStates)
        { }

        public bool Equals(State? other)
        {
            if (other == null)
            {
                return false;
            }

            return Count == other.Count
                   && this.All(
                       kv => other.ContainsKey(kv.Key) && Equals(other[kv.Key], kv.Value)
                       );
        }

        public override bool Equals(object? other)
        {
            return other is State state && Equals(state);
        }

        public override int GetHashCode()
        {
            return this.Aggregate(0, (current, kv) => current ^ kv.Key.GetHashCode() ^ kv.Value!.GetHashCode());
        }
    }
}
