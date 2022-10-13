using sernick.Input;

namespace sernick.Tokenizer.Lexer;

public interface ILexer<TCat>
{
    IEnumerable<Token<TCat>> Process(IInput input);
}
