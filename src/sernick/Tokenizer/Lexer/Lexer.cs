namespace sernick.Tokenizer.Lexer;

using Dfa;
using Input;

public class Lexer<TCat, TState> : ILexer<TCat>
{
    public Lexer(IReadOnlyDictionary<TCat, IDfa<TState>> categoryDfas)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Token<TCat>> Process(IInput input)
    {
        throw new NotImplementedException();
    }
}
