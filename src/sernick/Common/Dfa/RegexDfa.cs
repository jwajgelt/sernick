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

    public IEnumerable<IDfaWithConfig<Regex>.TransitionEdge> GetTransitionsFrom(Regex state)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IDfaWithConfig<Regex>.TransitionEdge> GetTransitionsTo(Regex state)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Regex> AcceptingStates => throw new NotImplementedException();
}

internal static class RegexDfaHelpers
{
    public static IEnumerable<char> PossibleFirstAtoms(this Regex regex)
    {
        switch (regex)
        {
            case AtomRegex atomRegex:
                return new[] { atomRegex.Character };

            case UnionRegex unionRegex:
                return unionRegex.Children.SelectMany(child => child.PossibleFirstAtoms()).ToHashSet();

            case ConcatRegex concatRegex:
                var withEpsilon = concatRegex.Children.TakeWhile(child => child.ContainsEpsilon());
                var firstWithoutEpsilon = concatRegex.Children.SkipWhile(child => child.ContainsEpsilon()).Take(1);
                return withEpsilon.Concat(firstWithoutEpsilon).SelectMany(child => child.PossibleFirstAtoms()).ToHashSet();

            case StarRegex starRegex:
                return starRegex.Child.PossibleFirstAtoms();

            default:
                throw new NotSupportedException("Unrecognized Regex class implementation");
        }
    }
}
