namespace sernickTest.Common.Dfa.Helpers;

using sernick.Common.Dfa;

public static class DfaExtensions
{
    public static TState Transition<TState>(this IDfa<TState, char> dfa, TState state, string word) =>
        word.Aggregate(state, dfa.Transition);
}
