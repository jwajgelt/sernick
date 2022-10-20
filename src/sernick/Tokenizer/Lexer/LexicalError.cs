using sernick.Diagnostics;
using sernick.Input;

namespace sernick.Tokenizer.Lexer;

public class LexicalError : IDiagnosticItem
{
    public LexicalError(ILocation start, ILocation end)
    {
        _start = start;
        _end = end;
    }

    private readonly ILocation _start;
    private readonly ILocation _end;

    public new string ToString()
    {
        return $"Lexical Error starting at: {_start.ToString()} and ending at: {_end.ToString()}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
