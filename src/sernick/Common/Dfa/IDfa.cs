namespace sernick.Common.Dfa;

public interface IDfa<TState, TAtom> where TAtom : IEquatable<TAtom>
{
    TState Transition(TState state, TAtom atom);
    bool Accepts(TState state);
    bool IsDead(TState state);

    TState Start { get; }

    IEnumerable<TransitionEdge<TState, TAtom>> GetTransitionsFrom(TState state);
    IEnumerable<TransitionEdge<TState, TAtom>> GetTransitionsTo(TState state);

    IEnumerable<TState> AcceptingStates { get; }
}
