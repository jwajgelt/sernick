namespace sernick.Tokenizer.Lexer;

using System.Text;
using Dfa;
using Diagnostics;
using Input;

public sealed class Lexer<TCat, TState> : ILexer<TCat>
    where TCat : notnull
{
    private readonly SumDfa _sumDfa;

    public Lexer(IReadOnlyDictionary<TCat, IDfa<TState>> categoryDfas)
    {
        _sumDfa = new SumDfa(categoryDfas);
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
        while (!input.CurrentLocation.Equals(input.End))
        {
            if (_sumDfa.IsDead(currentState))
            {
                if (lastAcceptingState != null)
                {
                    // return all matching token categories for this match
                    foreach (var category in _sumDfa.AcceptingCategories(lastAcceptingState.dfaStates))
                    {
                        yield return new Token<TCat>(category, text, lastAcceptingStart, lastAcceptingState.location);
                    }

                    // reset the input to the last end of the match
                    input.MoveTo(lastAcceptingState.location);
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
                    dfaStates: currentState,
                    location: input.CurrentLocation
                );
                text = textBuilder.ToString();
            }
        }

        // return the last match
        if (lastAcceptingState != null)
        {
            // return all matching token categories for this match
            foreach (var category in _sumDfa.AcceptingCategories(lastAcceptingState.dfaStates))
            {
                yield return new Token<TCat>(category, text, lastAcceptingStart, lastAcceptingState.location);
            }
        }
    }

    private sealed record LexerProcessingState(Dictionary<TCat, TState> dfaStates, ILocation location);

    private sealed class SumDfa : IDfa<Dictionary<TCat, TState>>
    {
        private readonly IReadOnlyDictionary<TCat, IDfa<TState>> _dfas;

        public SumDfa(IReadOnlyDictionary<TCat, IDfa<TState>> dfas)
        {
            _dfas = dfas;
            Start = dfas.ToDictionary(kv => kv.Key, kv => kv.Value.Start);
        }

        public Dictionary<TCat, TState> Start { get; }

        public bool Accepts(Dictionary<TCat, TState> state) =>
            state.Any(kv => _dfas[kv.Key].Accepts(kv.Value));

        public bool IsDead(Dictionary<TCat, TState> state) =>
            state.All(kv => _dfas[kv.Key].IsDead(kv.Value));

        public Dictionary<TCat, TState> Transition(Dictionary<TCat, TState> state, char atom) =>
            state.ToDictionary(
                kv => kv.Key,
                kv => _dfas[kv.Key].Transition(kv.Value, atom)
            );

        public IEnumerable<TCat> AcceptingCategories(Dictionary<TCat, TState> state) =>
            state.Where(kv => _dfas[kv.Key].Accepts(kv.Value)).Select(kv => kv.Key);
    }
}
