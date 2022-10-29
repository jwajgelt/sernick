using sernick.Utility;

namespace sernick.Common.Dfa;

public sealed class SumDfa<TCat, TState, TSymbol> : IDfa<IReadOnlyDictionary<TCat, TState>, TSymbol>
    where TCat : notnull
    where TSymbol : IEquatable<TSymbol>
{
    private readonly IReadOnlyDictionary<TCat, IDfa<TState, TSymbol>> _dfas;

    private Dictionary<IReadOnlyDictionary<TCat, TState>, List<TransitionEdge<IReadOnlyDictionary<TCat, TState>, TSymbol>>>?
        _transitionsToMap;

    private HashSet<IReadOnlyDictionary<TCat, TState>>? _acceptingStates;

    public SumDfa(IReadOnlyDictionary<TCat, IDfa<TState, TSymbol>> dfas)
    {
        _dfas = dfas;
        Start = dfas.ToDictionary(kv => kv.Key, kv => kv.Value.Start);
    }

    public IReadOnlyDictionary<TCat, TState> Start { get; }
    public IEnumerable<TransitionEdge<IReadOnlyDictionary<TCat, TState>, TSymbol>> GetTransitionsFrom(IReadOnlyDictionary<TCat, TState> state)
    {
        return _dfas
            .SelectMany(kv => kv.Value.GetTransitionsFrom(state[kv.Key]))
            .Select(transition => transition.Atom)
            .ToHashSet()    // only process each atom once
            .Select(atom =>
                new TransitionEdge<IReadOnlyDictionary<TCat, TState>, TSymbol>(state, Transition(state, atom), atom)
            );
    }

    public IEnumerable<TransitionEdge<IReadOnlyDictionary<TCat, TState>, TSymbol>> GetTransitionsTo(IReadOnlyDictionary<TCat, TState> state)
    {
        if (_transitionsToMap == null)
        {
            InitializeTransitionsDictionary();
        }

        return _transitionsToMap![state];
    }

    public IEnumerable<IReadOnlyDictionary<TCat, TState>> AcceptingStates
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

    public bool Accepts(IReadOnlyDictionary<TCat, TState> state) =>
        state.Any(kv => _dfas[kv.Key].Accepts(kv.Value));

    public bool IsDead(IReadOnlyDictionary<TCat, TState> state) =>
        state.All(kv => _dfas[kv.Key].IsDead(kv.Value));

    public IReadOnlyDictionary<TCat, TState> Transition(IReadOnlyDictionary<TCat, TState> state, TSymbol atom) =>
        state.ToDictionary(
            kv => kv.Key,
            kv => _dfas[kv.Key].Transition(kv.Value, atom)
        );

    public IEnumerable<TCat> AcceptingCategories(IReadOnlyDictionary<TCat, TState> state) =>
        state.Where(kv => _dfas[kv.Key].Accepts(kv.Value)).Select(kv => kv.Key);

    private void InitializeTransitionsDictionary()
    {
        _acceptingStates = new HashSet<IReadOnlyDictionary<TCat, TState>>();
        var transitionsToMap =
            new Dictionary<IReadOnlyDictionary<TCat, TState>, List<TransitionEdge<IReadOnlyDictionary<TCat, TState>, TSymbol>>>();
        var visited = new HashSet<IReadOnlyDictionary<TCat, TState>>();
        var queue = new Queue<IReadOnlyDictionary<TCat, TState>>();

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
            kv => kv.Value,
            new StateEqualityComparer()
            );
    }

    // by default, Dictionary doesn't seem to work as a Key,
    // so we implement our own EqualityComparer
    private class StateEqualityComparer : IEqualityComparer<IReadOnlyDictionary<TCat, TState>>
    {
        public bool Equals(IReadOnlyDictionary<TCat, TState>? x, IReadOnlyDictionary<TCat, TState>? y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Count == y.Count && x.All(kv => y.ContainsKey(kv.Key) && Equals(y[kv.Key], kv.Value));
        }

        public int GetHashCode(IReadOnlyDictionary<TCat, TState> obj)
        {
            return obj.Aggregate(0, (current, kv) => current ^ kv.Key.GetHashCode() ^ kv.Value!.GetHashCode());
        }
    }
}
