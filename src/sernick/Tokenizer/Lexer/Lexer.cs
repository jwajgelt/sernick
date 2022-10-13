namespace sernick.Tokenizer.Lexer;

using System.Text;
using Dfa;
using Input;

public class Lexer<TCat, TState> : ILexer<TCat>
    where TCat : notnull
{
    private readonly IReadOnlyDictionary<TCat, IDfa<TState>> categoryDfas;

    public Lexer(IReadOnlyDictionary<TCat, IDfa<TState>> categoryDfas)
    {
        this.categoryDfas = categoryDfas;
    }

    public IEnumerable<Token<TCat>> Process(IInput input)
    {
        input.MoveTo(input.Start);

        var startingStates = categoryDfas.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Start
            );

        // holds the current states for all DFAs
        var currentStates = startingStates.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
            );

        Dictionary<TCat, TState>? lastAcceptingStates = null;
        var lastAcceptingStart = input.Start;
        ILocation? lastAcceptingEnd = null;
        var text = "";
        var textBuilder = new StringBuilder();

        // loop over the input
        while (!input.CurrentLocation.Equals(input.End))
        {
            if (allDfasDead(currentStates))
            {
                // NOTE: they're either both null or not null,
                // but this appeases the compiler's nullability checks
                if (lastAcceptingStates != null && lastAcceptingEnd != null)
                {
                    // return all matching token categories for this match
                    foreach (var (category, state) in lastAcceptingStates)
                    {
                        var dfa = categoryDfas[category];
                        if (dfa.Accepts(state))
                        {
                            yield return new Token<TCat>(category, text, lastAcceptingStart, lastAcceptingEnd);
                        }
                    }

                    // reset the input to the last end of the match
                    input.MoveTo(lastAcceptingEnd);
                }
                // If we matched, we start the next match from the position
                // we just reset to.
                // Otherwise, all DFAs failed to match on a substring,
                // and we begin matching again from the current position
                currentStates = startingStates.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value
                );

                lastAcceptingStart = input.CurrentLocation;

                // reset the local state
                textBuilder.Clear();
                lastAcceptingStates = null;
                lastAcceptingEnd = null;
            }

            // advance each DFA by the current character
            var current = input.Current;
            foreach (var (category, state) in currentStates)
            {
                var dfa = categoryDfas[category];
                var nextState = dfa.Transition(state, current);
                currentStates[category] = nextState;
            }
            // and add it to the current token's text
            textBuilder.Append(current);

            // advance the input
            input.MoveNext();

            // check if we're one position after the match
            var anyAccepts = anyDfaAccepts(currentStates);

            if (anyAccepts)
            {
                lastAcceptingEnd = input.CurrentLocation;
                // clone the current states
                lastAcceptingStates = currentStates.ToDictionary(kv => kv.Key, kv => kv.Value);
                text = textBuilder.ToString();
            }
        }

        // return the last match
        if (lastAcceptingStates != null && lastAcceptingEnd != null)
        {
            // return all matching token categories for this match
            foreach (var (category, state) in lastAcceptingStates)
            {
                var dfa = categoryDfas[category];
                if (dfa.Accepts(state))
                {
                    yield return new Token<TCat>(category, text, lastAcceptingStart, lastAcceptingEnd);
                }
            }
        }
    }

    private bool anyDfaAccepts(Dictionary<TCat, TState> currentStates) => currentStates.Any(
        (kv) =>
        {
            var dfa = categoryDfas[kv.Key];
            return dfa.Accepts(kv.Value);
        }
    );

    private bool allDfasDead(Dictionary<TCat, TState> currentStates) => currentStates.All(
        (kv) =>
        {
            var dfa = categoryDfas[kv.Key];
            return dfa.IsDead(kv.Value);
        }
    );
}
