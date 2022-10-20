namespace sernick.Common.Dfa;

public interface IDfaWithConfig<TState, TAtom> : IDfa<TState, TAtom> where TAtom : IEquatable<TAtom>
{
    IEnumerable<TransitionEdge<TState, TAtom>> GetTransitionsFrom(TState state);
    IEnumerable<TransitionEdge<TState, TAtom>> GetTransitionsTo(TState state);

    IEnumerable<TState> AcceptingStates { get; }
}
