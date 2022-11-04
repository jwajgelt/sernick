namespace sernickTest.Common.Dfa.Helpers;

using sernick.Common.Dfa;

public static class DfaExtensions
{
    public static TState Transition<TState>(this IDfa<TState, char> dfa, TState state, string word) =>
        word.Aggregate(state, dfa.Transition);

    /// <returns>True if dfa accepts whole text when starting from the dfa.start state</returns>
    public static bool AcceptsText<TState>(this IDfa<TState, char> dfa, string text) =>
        dfa.Accepts(dfa.Transition(dfa.Start, text));
}
