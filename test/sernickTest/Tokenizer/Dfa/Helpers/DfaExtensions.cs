namespace sernickTest.Tokenizer.Dfa.Helpers;

using sernick.Tokenizer.Dfa;

public static class DfaExtensions
{
    public static TState Transition<TState>(this IDfa<TState> dfa, TState state, string word) =>
        word.Aggregate(state, dfa.Transition);
}
