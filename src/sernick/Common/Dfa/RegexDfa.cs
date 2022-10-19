namespace sernick.Common.Dfa;

using Regex;

public sealed class RegexDfa : IDfaWithConfig<Regex>
{
    public RegexDfa(Regex regex)
    {
        Start = regex;
    }

    public Regex Start { get; }

    public bool Accepts(Regex state) => state.ContainsEpsilon();

    public bool IsDead(Regex state) => state.Equals(Regex.Empty);

    public Regex Transition(Regex state, char atom) => state.Derivative(atom);

    public IEnumerable<IDfaWithConfig<Regex>.TransitionEdge> GetTransitionFrom(Regex state)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IDfaWithConfig<Regex>.TransitionEdge> GetTransitionsTo(Regex state)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Regex> AcceptingStates => throw new NotImplementedException();
}
