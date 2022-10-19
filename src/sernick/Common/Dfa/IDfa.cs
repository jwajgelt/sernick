namespace sernick.Common.Dfa;

public interface IDfa<TState>
{
    TState Transition(TState state, char atom);
    bool Accepts(TState state);
    bool IsDead(TState state);

    TState Start { get; }
}
