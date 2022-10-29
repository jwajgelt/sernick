namespace sernickTest.Tokenizer.Lexer.Helpers;

using sernick.Common.Dfa;

internal sealed class FakeDfa : IDfa<int, char>
{
    private readonly IReadOnlyDictionary<(int, char), int> _transitions;
    private readonly IReadOnlySet<int> _accepting;

    private const int DEAD_STATE = -1;

    public FakeDfa(
        IReadOnlyDictionary<(int, char), int> transitions,
        int start,
        IReadOnlySet<int> accepting
    ) => (_transitions, Start, _accepting) = (transitions, start, accepting);

    public int Start { get; }

    public IEnumerable<int> AcceptingStates => _accepting;

    public bool Accepts(int state) => _accepting.Contains(state);

    public bool IsDead(int state) => state == DEAD_STATE;

    public int Transition(int state, char atom)
    {
        return _transitions.TryGetValue((state, atom), out var next)
            ? next
            : DEAD_STATE;
    }

    public IEnumerable<TransitionEdge<int, char>> GetTransitionsFrom(int state)
    {
        return _transitions.Where(t => t.Key.Item1 == state)
            .Select(t => new TransitionEdge<int, char>(t.Key.Item1, t.Value, t.Key.Item2));
    }

    public IEnumerable<TransitionEdge<int, char>> GetTransitionsTo(int state)
    {
        return _transitions.Where(t => t.Value == state)
            .Select(t => new TransitionEdge<int, char>(t.Key.Item1, t.Value, t.Key.Item2));
    }
}
