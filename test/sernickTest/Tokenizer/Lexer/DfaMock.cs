namespace sernickTest.Tokenizer.Lexer;

internal class DfaMock : sernick.Tokenizer.Dfa.IDfa<int>
{
    private readonly IReadOnlyDictionary<(int, char), int> transitions;
    private readonly IReadOnlySet<int> accepting;

    public DfaMock(
        IReadOnlyDictionary<(int, char), int> transitions,
        int start,
        IReadOnlySet<int> accepting
    )
    {
        this.transitions = transitions;
        this.accepting = accepting;
        Start = start;
    }

    public int Start { get; init; }

    public bool Accepts(int state)
    {
        return accepting.Contains(state);
    }

    public bool IsDead(int state)
    {
        // `-1` indicates a dead state
        return state == -1;
    }

    public int Transition(int state, char atom)
    {
        int next;
        if (transitions.TryGetValue((state, atom), out next))
        {
            return next;
        }
        else
        {
            return -1;
        }
    }
}
