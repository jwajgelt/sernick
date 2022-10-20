namespace sernick.Common.Dfa;

using Regex;
public sealed class RegexDfa<TAtom> : IDfaWithConfig<Regex<TAtom>, TAtom> where TAtom : IEquatable<TAtom>
{
    private RegexDfa(
        Regex<TAtom> regex,
        IEnumerable<Regex<TAtom>> acceptingStates,
        Dictionary<Regex<TAtom>, List<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge>> transitionsToMap,
        Dictionary<Regex<TAtom>, List<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge>> transitionsFromMap
        ) => (Start, AcceptingStates, _transitionsToMap, _transitionsFromMap) = (regex, acceptingStates, transitionsToMap, transitionsFromMap);

    public Regex<TAtom> Start { get; }

    public bool Accepts(Regex<TAtom> state) => state.ContainsEpsilon();

    public bool IsDead(Regex<TAtom> state) => state.Equals(Regex<TAtom>.Empty);

    public Regex<TAtom> Transition(Regex<TAtom> state, TAtom atom) => state.Derivative(atom);

    public IEnumerable<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge> GetTransitionsFrom(Regex<TAtom> state) =>
        _transitionsFromMap[state];

    public IEnumerable<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge> GetTransitionsTo(Regex<TAtom> state) => _transitionsToMap[state];
    public IEnumerable<Regex<TAtom>> AcceptingStates { get; }
    private readonly Dictionary<Regex<TAtom>, List<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge>> _transitionsToMap;
    private readonly Dictionary<Regex<TAtom>, List<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge>> _transitionsFromMap;

    public static IDfaWithConfig<Regex<TAtom>, TAtom> FromRegex(Regex<TAtom> start)
    {
        var acceptingStates = new List<Regex<TAtom>>();
        var visited = new HashSet<Regex<TAtom>>();
        var queue = new Queue<Regex<TAtom>>();
        var transitionsToMap = new Dictionary<Regex<TAtom>, List<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge>>();
        var transitionsFromMap = new Dictionary<Regex<TAtom>, List<IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge>>();

        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            if (currentState.ContainsEpsilon())
            {
                acceptingStates.Add(currentState);
            }

            foreach (var atom in currentState.PossibleFirstAtoms())
            {
                var nextState = currentState.Derivative(atom);
                var edge = new IDfaWithConfig<Regex<TAtom>, TAtom>.TransitionEdge(currentState, nextState, atom);

                transitionsToMap.GetOrAddList(nextState).Add(edge);
                transitionsFromMap.GetOrAddList(currentState).Add(edge);

                if (visited.Contains(nextState))
                {
                    continue;
                }

                queue.Enqueue(nextState);
                visited.Add(nextState);
            }
        }

        return new RegexDfa<TAtom>(start, acceptingStates, transitionsToMap, transitionsFromMap);
    }
}

internal static class RegexDfaHelpers
{
    public static IEnumerable<TAtom> PossibleFirstAtoms<TAtom>(this Regex<TAtom> regex)
        where TAtom : IEquatable<TAtom>
    {
        switch (regex)
        {
            case AtomRegex<TAtom> atomRegex:
                return new[] { atomRegex.Atom };

            case UnionRegex<TAtom> unionRegex:
                return unionRegex.Children.SelectMany(child => child.PossibleFirstAtoms()).ToHashSet();

            case ConcatRegex<TAtom> concatRegex:
                var withEpsilon = concatRegex.Children.TakeWhile(child => child.ContainsEpsilon());
                var firstWithoutEpsilon = concatRegex.Children.SkipWhile(child => child.ContainsEpsilon()).Take(1);
                return withEpsilon.Concat(firstWithoutEpsilon).SelectMany(child => child.PossibleFirstAtoms()).ToHashSet();

            case StarRegex<TAtom> starRegex:
                return starRegex.Child.PossibleFirstAtoms();

            default:
                throw new NotSupportedException("Unrecognized Regex class implementation");
        }
    }

    public static List<TValue> GetOrAddList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key)
    {
        dictionary.TryAdd(key, new List<TValue>());
        return dictionary[key];
    }
}
