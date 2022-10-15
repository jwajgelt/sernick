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
        return state.ContainsEpsilon();
    }

    public bool IsDead(Regex state)
    {
        return (state is UnionRegex unionRegex) && (unionRegex.Children.Count() == 0); 
    }

    public Regex Transition(Regex state, char atom)
    {
        return state.Derivative(atom);
    }
}
