namespace sernick.Tokenizer.Dfa;

public interface IDfa<TState>
{
    TState Transition(TState state, char atom);
    bool Accepts(TState state);
    bool IsDead(TState state);

    TState Transition(TState state, string word);
    TState Start { get; }
}
