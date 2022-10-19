namespace sernick.Tokenizer.Lexer;

using Diagnostics;
using Input;

public interface ILexer<TCat>
{
    IEnumerable<Token<TCat>> Process(IInput input, IDiagnostics diagnostics);
}
