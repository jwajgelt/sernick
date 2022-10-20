using sernick.Diagnostics;
using sernick.Input;

namespace sernick.Tokenizer.Lexer;

public class LexicalError : IDiagnosticItem
{
    public LexicalError(ILocation start, ILocation end)
    {
        Start = start;
        End = end;
    }

    private ILocation Start;
    private ILocation End;
    
    public new string ToString()
    {
        return $"Lexical Error starting at: {Start.ToString()} and ending at: {End.ToString()}";
    }
    
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
