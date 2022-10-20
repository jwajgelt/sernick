namespace sernick.Common.Dfa;

public interface IDfaWithConfig<TState, TAtom> : IDfa<TState, TAtom> where TAtom : IEquatable<TAtom>
{
    IEnumerable<TransitionEdge> GetTransitionsFrom(TState state);
    IEnumerable<TransitionEdge> GetTransitionsTo(TState state);

    IEnumerable<TState> AcceptingStates { get; }

    public sealed record TransitionEdge(TState From, TState To, TAtom Atom);
}
