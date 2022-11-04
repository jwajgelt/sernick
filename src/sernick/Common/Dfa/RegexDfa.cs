namespace sernick.Common.Dfa;

using Regex;
using Utility;

public sealed class RegexDfa<TAtom> : IDfa<Regex<TAtom>, TAtom> where TAtom : IEquatable<TAtom>
{
    private RegexDfa(
        Regex<TAtom> regex,
        IEnumerable<Regex<TAtom>> acceptingStates,
        Dictionary<Regex<TAtom>, List<TransitionEdge<Regex<TAtom>, TAtom>>> transitionsToMap,
        Dictionary<Regex<TAtom>, List<TransitionEdge<Regex<TAtom>, TAtom>>> transitionsFromMap
        ) => (Start, AcceptingStates, _transitionsToMap, _transitionsFromMap) = (regex, acceptingStates, transitionsToMap, transitionsFromMap);

    public Regex<TAtom> Start { get; }

    public bool Accepts(Regex<TAtom> state) => state.ContainsEpsilon();

    public bool IsDead(Regex<TAtom> state) => state.Equals(Regex<TAtom>.Empty);

    public Regex<TAtom> Transition(Regex<TAtom> state, TAtom atom) => state.Derivative(atom);

    public IEnumerable<TransitionEdge<Regex<TAtom>, TAtom>> GetTransitionsFrom(Regex<TAtom> state) =>
        _transitionsFromMap.TryGetValue(state, out var value)
            ? value : Enumerable.Empty<TransitionEdge<Regex<TAtom>, TAtom>>();

    public IEnumerable<TransitionEdge<Regex<TAtom>, TAtom>> GetTransitionsTo(Regex<TAtom> state) =>
        _transitionsToMap.TryGetValue(state, out var value)
            ? value : Enumerable.Empty<TransitionEdge<Regex<TAtom>, TAtom>>();
    public IEnumerable<Regex<TAtom>> AcceptingStates { get; }
    private readonly Dictionary<Regex<TAtom>, List<TransitionEdge<Regex<TAtom>, TAtom>>> _transitionsToMap;
    private readonly Dictionary<Regex<TAtom>, List<TransitionEdge<Regex<TAtom>, TAtom>>> _transitionsFromMap;

    public static IDfa<Regex<TAtom>, TAtom> FromRegex(Regex<TAtom> start)
    {
        var acceptingStates = new List<Regex<TAtom>>();
        var visited = new HashSet<Regex<TAtom>>();
        var queue = new Queue<Regex<TAtom>>();
        var transitionsToMap = new Dictionary<Regex<TAtom>, List<TransitionEdge<Regex<TAtom>, TAtom>>>();
        var transitionsFromMap = new Dictionary<Regex<TAtom>, List<TransitionEdge<Regex<TAtom>, TAtom>>>();

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
                var edge = new TransitionEdge<Regex<TAtom>, TAtom>(currentState, nextState, atom);

                transitionsToMap.GetOrAddEmpty(nextState).Add(edge);
                transitionsFromMap.GetOrAddEmpty(currentState).Add(edge);

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
}
