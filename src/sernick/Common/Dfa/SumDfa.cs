using System.Collections;

namespace sernick.Common.Dfa;

using Utility;

public sealed class SumDfa<TCat, TState, TSymbol> : IDfa<SumDfa<TCat, TState, TSymbol>.SumDfaState, TSymbol>
    where TCat : notnull
    where TSymbol : IEquatable<TSymbol>
{
    private readonly IReadOnlyDictionary<TCat, IDfa<TState, TSymbol>> _dfas;

    private Dictionary<SumDfaState, List<TransitionEdge<SumDfaState, TSymbol>>>?
        _transitionsToMap;

    private HashSet<SumDfaState>? _acceptingStates;

    public SumDfa(IReadOnlyDictionary<TCat, IDfa<TState, TSymbol>> dfas)
    {
        _dfas = dfas;
        Start = new SumDfaState(dfas.ToDictionary(kv => kv.Key, kv => kv.Value.Start));
    }

    public SumDfaState Start { get; }
    public IEnumerable<TransitionEdge<SumDfaState, TSymbol>> GetTransitionsFrom(SumDfaState state)
    {
        return _dfas
            .SelectMany(kv => kv.Value.GetTransitionsFrom(state[kv.Key]))
            .Select(transition => transition.Atom)
            .ToHashSet()    // only process each atom once
            .Select(atom =>
                new TransitionEdge<SumDfaState, TSymbol>(state, Transition(state, atom), atom)
            );
    }

    public IEnumerable<TransitionEdge<SumDfaState, TSymbol>> GetTransitionsTo(SumDfaState state)
    {
        if (_transitionsToMap == null)
        {
            InitializeTransitionsDictionary();
        }

        return _transitionsToMap![state];
    }

    public IEnumerable<SumDfaState> AcceptingStates
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

    public bool Accepts(SumDfaState state) =>
        state.Any(kv => _dfas[kv.Key].Accepts(kv.Value));

    public bool IsDead(SumDfaState state) =>
        state.All(kv => _dfas[kv.Key].IsDead(kv.Value));

    public SumDfaState Transition(SumDfaState state, TSymbol atom) =>
        new(state.ToDictionary(
            kv => kv.Key,
            kv => _dfas[kv.Key].Transition(kv.Value, atom)
        ));

    public IEnumerable<TCat> AcceptingCategories(SumDfaState state) =>
        state.Where(kv => _dfas[kv.Key].Accepts(kv.Value)).Select(kv => kv.Key);

    private void InitializeTransitionsDictionary()
    {
        _acceptingStates = new HashSet<SumDfaState>();
        var transitionsToMap =
            new Dictionary<SumDfaState, List<TransitionEdge<SumDfaState, TSymbol>>>();
        var visited = new HashSet<SumDfaState>();
        var queue = new Queue<SumDfaState>();

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

    public class SumDfaState : IEquatable<SumDfaState>, IEnumerable<KeyValuePair<TCat, TState>>
    {
        public SumDfaState(IReadOnlyDictionary<TCat, TState> partStates)
        {
            _partStates = partStates;
        }

        public TState this[TCat cat] => _partStates[cat];

        public bool Equals(SumDfaState? other)
        {
            if (other == null)
            {
                return false;
            }

            return _partStates.Count == other._partStates.Count
                   && _partStates.All(
                       kv => other._partStates.ContainsKey(kv.Key) && Equals(other._partStates[kv.Key], kv.Value)
                       );
        }

        public IEnumerator<KeyValuePair<TCat, TState>> GetEnumerator()
        {
            return _partStates.GetEnumerator();
        }

        public override int GetHashCode()
        {
            return _partStates.Aggregate(0, (current, kv) => current ^ kv.Key.GetHashCode() ^ kv.Value!.GetHashCode());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_partStates).GetEnumerator();
        }

        private readonly IReadOnlyDictionary<TCat, TState> _partStates;
    }
}
