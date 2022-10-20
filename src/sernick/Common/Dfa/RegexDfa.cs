namespace sernick.Common.Dfa;

using Regex;
using TransitionEdge = IDfaWithConfig<Regex.Regex>.TransitionEdge;

public sealed class RegexDfa : IDfaWithConfig<Regex>
{
    public RegexDfa(Regex regex)
    {
        Start = regex;
        (AcceptingStates, _transitionsToMap, _transitionsFromMap) = this.Preprocess();
    }

    public Regex Start { get; }

    public bool Accepts(Regex state) => state.ContainsEpsilon();

    public bool IsDead(Regex state) => state.Equals(Regex.Empty);

    public Regex Transition(Regex state, char atom) => state.Derivative(atom);

    public IEnumerable<TransitionEdge> GetTransitionsFrom(Regex state) =>
        _transitionsFromMap[state];

    public IEnumerable<TransitionEdge> GetTransitionsTo(Regex state) => _transitionsToMap[state];
    public IEnumerable<Regex> AcceptingStates { get; }
    private readonly Dictionary<Regex, List<TransitionEdge>> _transitionsToMap;
    private readonly Dictionary<Regex, List<TransitionEdge>> _transitionsFromMap;
}

internal static class RegexDfaHelpers
{
    private static IEnumerable<char> PossibleFirstAtoms(this Regex regex)
    {
        switch (regex)
        {
            case AtomRegex atomRegex:
                return new[] { atomRegex.Character };

            case UnionRegex unionRegex:
                return unionRegex.Children.SelectMany(child => child.PossibleFirstAtoms()).ToHashSet();

            case ConcatRegex concatRegex:
                var withEpsilon = concatRegex.Children.TakeWhile(child => child.ContainsEpsilon());
                var firstWithoutEpsilon = concatRegex.Children.SkipWhile(child => child.ContainsEpsilon()).Take(1);
                return withEpsilon.Concat(firstWithoutEpsilon).SelectMany(child => child.PossibleFirstAtoms()).ToHashSet();

            case StarRegex starRegex:
                return starRegex.Child.PossibleFirstAtoms();

            default:
                throw new NotSupportedException("Unrecognized Regex class implementation");
        }
    }

    private static List<TValue> GetOrAddList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key)
    {
        dictionary.TryAdd(key, new List<TValue>());
        return dictionary[key];
    }

    public static (
        IEnumerable<Regex> AcceptingStates,
        Dictionary<Regex, List<TransitionEdge>> transitionsToMap,
        Dictionary<Regex, List<TransitionEdge>> transitionsFromMap
        ) Preprocess(this RegexDfa dfa)
    {
        var acceptingStates = new List<Regex>();
        var visited = new HashSet<Regex>();
        var queue = new Queue<Regex>();
        var transitionsToMap = new Dictionary<Regex, List<TransitionEdge>>();
        var transitionsFromMap = new Dictionary<Regex, List<TransitionEdge>>();

        queue.Enqueue(dfa.Start);
        visited.Add(dfa.Start);
        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            if (dfa.Accepts(currentState))
            {
                acceptingStates.Add(currentState);
            }

            foreach (var atom in currentState.PossibleFirstAtoms())
            {
                var nextState = dfa.Transition(currentState, atom);
                var edge = new TransitionEdge(currentState, nextState, atom);

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

        return (acceptingStates, transitionsToMap, transitionsFromMap);
    }
}
