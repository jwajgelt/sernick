namespace sernick.Grammar.Dfa;

using Tokenizer.Dfa;

public sealed class DfaGrammar<TSymbol, TDfaState>
{
    /// <summary>
    /// Start symbol of the grammar.
    /// </summary>
    public TSymbol Start { get; init; }

    /// <summary>
    /// Dictionary of all productions in grammar (right-hand side is a corresponding DFA)
    /// </summary>
    public IReadOnlyDictionary<TSymbol, IDfa<TDfaState>> Productions { get; init; }
}
