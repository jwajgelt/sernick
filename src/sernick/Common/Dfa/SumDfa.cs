using sernick.Utility;

namespace sernick.Common.Dfa;

public sealed class SumDfa<TCat, TState> : IDfa<Dictionary<TCat, TState>, char>
    where TCat : notnull
{
    private readonly IReadOnlyDictionary<TCat, IDfa<TState, char>> _dfas;

    private Dictionary<Dictionary<TCat, TState>, List<TransitionEdge<Dictionary<TCat, TState>, char>>>?
        _transitionsToMap;

    private HashSet<Dictionary<TCat, TState>>? _acceptingStates;

    public SumDfa(IReadOnlyDictionary<TCat, IDfa<TState, char>> dfas)
    {
        _dfas = dfas;
        Start = dfas.ToDictionary(kv => kv.Key, kv => kv.Value.Start);
    }

    public Dictionary<TCat, TState> Start { get; }
    public IEnumerable<TransitionEdge<Dictionary<TCat, TState>, char>> GetTransitionsFrom(Dictionary<TCat, TState> state)
    {
        return _dfas
            .SelectMany(kv => kv.Value.GetTransitionsFrom(state[kv.Key]))
            .Select(transition => transition.Atom)
            .ToHashSet()    // only process each atom once
            .Select(atom =>
                new TransitionEdge<Dictionary<TCat, TState>, char>(state, Transition(state, atom), atom)
            );
    }

    public IEnumerable<TransitionEdge<Dictionary<TCat, TState>, char>> GetTransitionsTo(Dictionary<TCat, TState> state)
    {
        if (_transitionsToMap == null)
        {
            InitializeTransitionsDictionary();
        }

        return _transitionsToMap![state];
    }

    public IEnumerable<Dictionary<TCat, TState>> AcceptingStates
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

    public bool Accepts(Dictionary<TCat, TState> state) =>
        state.Any(kv => _dfas[kv.Key].Accepts(kv.Value));

    public bool IsDead(Dictionary<TCat, TState> state) =>
        state.All(kv => _dfas[kv.Key].IsDead(kv.Value));

    public Dictionary<TCat, TState> Transition(Dictionary<TCat, TState> state, char atom) =>
        state.ToDictionary(
            kv => kv.Key,
            kv => _dfas[kv.Key].Transition(kv.Value, atom)
        );

    public IEnumerable<TCat> AcceptingCategories(Dictionary<TCat, TState> state) =>
        state.Where(kv => _dfas[kv.Key].Accepts(kv.Value)).Select(kv => kv.Key);

    private void InitializeTransitionsDictionary()
    {
        _acceptingStates = new HashSet<Dictionary<TCat, TState>>();
        var transitionsToMap =
            new Dictionary<Dictionary<TCat, TState>, List<TransitionEdge<Dictionary<TCat, TState>, char>>>();
        var visited = new HashSet<Dictionary<TCat, TState>>();
        var queue = new Queue<Dictionary<TCat, TState>>();

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
    private class StateEqualityComparer : IEqualityComparer<Dictionary<TCat, TState>>
    {
        public bool Equals(Dictionary<TCat, TState>? x, Dictionary<TCat, TState>? y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Count == y.Count && x.All(kv => y.ContainsKey(kv.Key) && y[kv.Key].Equals(kv.Value));
        }

        public int GetHashCode(Dictionary<TCat, TState> obj)
        {
            return obj.Aggregate(0, (current, kv) => current ^ kv.Key.GetHashCode() ^ kv.Value!.GetHashCode());
        }
    }
}
