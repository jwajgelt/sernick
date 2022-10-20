namespace sernick.Common.Dfa;

public interface IDfa<TState, TAtom> where TAtom : IEquatable<TAtom>
{
    TState Transition(TState state, TAtom atom);
    bool Accepts(TState state);
    bool IsDead(TState state);

    TState Start { get; }
}
