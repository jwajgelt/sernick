namespace sernick.Tokenizer.Dfa;

using Regex;

public sealed class RegexDfa : IDfa<Regex>
{
    public RegexDfa(Regex regex)
    {
        Start = regex;
    }
    public Regex Start { get; }

    public bool Accepts(Regex state)
    {
        throw new NotImplementedException();
    }

    public bool IsDead(Regex state)
    {
        throw new NotImplementedException();
    }

    public Regex Transition(Regex state, char atom)
    {
        throw new NotImplementedException();
    }
}
