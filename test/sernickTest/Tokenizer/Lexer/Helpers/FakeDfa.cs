namespace sernickTest.Tokenizer.Lexer.Helpers;

internal class FakeDfa : sernick.Tokenizer.Dfa.IDfa<int>
{
    private readonly IReadOnlyDictionary<(int, char), int> _transitions;
    private readonly IReadOnlySet<int> _accepting;

    private const int DEAD_STATE = -1;

    public FakeDfa(
        IReadOnlyDictionary<(int, char), int> transitions,
        int start,
        IReadOnlySet<int> accepting
    )
    {
        _transitions = transitions;
        _accepting = accepting;
        Start = start;
    }

    public int Start { get; init; }

    public bool Accepts(int state)
    {
        return _accepting.Contains(state);
    }

    public bool IsDead(int state)
    {
        return state == DEAD_STATE;
    }

    public int Transition(int state, char atom)
    {
        return _transitions.TryGetValue((state, atom), out var next)
            ? next
            : DEAD_STATE;
    }
}
