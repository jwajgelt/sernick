namespace sernick.Tokenizer.Lexer;

using Input;

public interface ILexer<TCat>
{
    IEnumerable<Token<TCat>> Process(IInput input);
}
