namespace sernick.Tokenizer.Dfa;

using Regex;

public class RegexDfa : IDfa<Regex>
{
    public Regex Start => throw new NotImplementedException();

    public bool Accepts(Regex state)
    {
        throw new NotImplementedException();
    }

    public bool IsDead(Regex state)
    {
        throw new NotImplementedException();
    }

    public Regex Transition(Regex state, Char atom)
    {
        throw new NotImplementedException();
    }
}
