namespace sernick.Tokenizer.Lexer;

using Diagnostics;
using Input;

public sealed record LexicalError(ILocation Start, ILocation End) : IDiagnosticItem
{
    public bool Equals(IDiagnosticItem? other) => other is LexicalError && other.Severity == Severity && other.ToString() == ToString();

    public override string ToString()
    {
        return $"Lexical error starting at {Start} and ending at {End}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
