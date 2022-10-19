namespace sernickTest.Tokenizer.Lexer.Helpers;

using sernick.Tokenizer.Dfa;

internal sealed class FakeDfa : IDfa<int>
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

    public bool Accepts(int state) => _accepting.Contains(state);

    public bool IsDead(int state) => state == DEAD_STATE;

    public int Transition(int state, char atom)
    {
        return _transitions.TryGetValue((state, atom), out var next)
            ? next
            : DEAD_STATE;
    }
}
