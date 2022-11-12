namespace sernick.Tokenizer.Lexer;

using System.Text;
using Common.Dfa;
using Diagnostics;
using Input;
using Utility;

public sealed class Lexer<TCat, TState> : ILexer<TCat>
    where TCat : notnull
{
    private readonly SumDfa<TCat, TState, char> _sumDfa;

    public Lexer(IReadOnlyDictionary<TCat, IDfa<TState, char>> categoryDfas)
    {
        _sumDfa = new SumDfa<TCat, TState, char>(categoryDfas);
    }

    public IEnumerable<Token<TCat>> Process(IInput input, IDiagnostics diagnostics)
    {
        input.MoveTo(input.Start);
        var currentState = _sumDfa.Start;

        var lastAcceptingStart = input.Start;
        LexerProcessingState? lastAcceptingState = null;
        var text = "";
        var textBuilder = new StringBuilder();

        // loop over the input
        while (true)
        {
            if (_sumDfa.IsDead(currentState))
            {
                if (lastAcceptingState != null)
                {
                    // return the matching token category with the highest priority for this match
                    var matchingCategory = _sumDfa.AcceptingCategories(lastAcceptingState.DfaStates).Min()!;
                    // matching category is non-null, since `_sumDfa` accepted `lastAcceptingState`
                    yield return new Token<TCat>(matchingCategory, text, (lastAcceptingStart, lastAcceptingState.Location));

                    // reset the input to the last end of the match
                    input.MoveTo(lastAcceptingState.Location);
                }
                else
                {
                    // report the error
                    var error = new LexicalError(lastAcceptingStart, input.CurrentLocation);
                    diagnostics.Report(error);
                }

                // If we matched, we start the next match from the position
                // we just reset to.
                // Otherwise, all DFAs failed to match on a substring,
                // and we begin matching again from the current position
                currentState = _sumDfa.Start;
                lastAcceptingStart = input.CurrentLocation;

                // reset the local state
                textBuilder.Clear();
                lastAcceptingState = null;
            }

            if (input.CurrentLocation.Equals(input.End))
            {
                break;
            }

            // advance the sum-DFA by the current character
            var current = input.Current;
            currentState = _sumDfa.Transition(currentState, current);
            // and add it to the current token's text
            textBuilder.Append(current);

            input.MoveNext();

            // check if we're one position after the match
            var anyAccepts = _sumDfa.Accepts(currentState);
            if (anyAccepts)
            {
                lastAcceptingState = new LexerProcessingState(
                    DfaStates: currentState,
                    Location: input.CurrentLocation
                );
                text = textBuilder.ToString();
            }
        }

        // return the last match
        if (lastAcceptingState == null)
        {
            yield break;
        }

        // return the matching token category with the highest priority for this match
        var category = _sumDfa.AcceptingCategories(lastAcceptingState.DfaStates).Min()!;
        // matching category is non-null, since `_sumDfa` accepted `lastAcceptingState`
        yield return new Token<TCat>(category, text, (lastAcceptingStart, lastAcceptingState.Location));
    }

    private sealed record LexerProcessingState(SumDfa<TCat, TState, char>.State DfaStates, ILocation Location);
}
