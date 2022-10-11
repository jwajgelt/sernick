namespace sernick.Tokenizer.Dfa;

public interface IDfa<TState>
{
    TState Transition(TState state, Char atom);
    bool IsAccepting(TState state);
    bool IsDead(TState state);
    TState Start{ get; }
}
