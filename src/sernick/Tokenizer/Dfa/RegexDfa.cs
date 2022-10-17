namespace sernick.Tokenizer.Dfa;
using Regex;

public sealed class RegexDfa : IDfa<Regex>
{
    public RegexDfa(Regex regex)
    {
        Start = regex;
    }
    public Regex Start { get; }

    public bool Accepts(Regex state) => state.ContainsEpsilon();

    public bool IsDead(Regex state) => state.Equals(Regex.Empty);

    public Regex Transition(Regex state, char atom) => state.Derivative(atom);

    public Regex Transition(Regex state, string word) => word.Aggregate(state, (state, c) => Transition(state, c));
}
