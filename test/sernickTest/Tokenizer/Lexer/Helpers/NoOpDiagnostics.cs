namespace sernickTest.Tokenizer.Lexer.Helpers;

using sernick.Diagnostics;

public class NoOpDiagnostics : IDiagnostics
{
    public void Report(IDiagnosticItem diagnosticItem)
    {
    }
}
