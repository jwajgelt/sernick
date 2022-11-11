namespace sernick.Tokenizer.Lexer;

using Diagnostics;
using Input;

public sealed record LexicalError(ILocation Start, ILocation End) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Lexical error starting at {Start} and ending at {End}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
