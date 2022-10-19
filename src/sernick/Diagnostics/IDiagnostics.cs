namespace sernick.Diagnostics;

public interface IDiagnostics
{
    void Report(IDiagnosticItem diagnosticItem);
}

public interface IDiagnosticItem
{
    string ToString();

    DiagnosticItemSeverity Severity { get; }
}

public enum DiagnosticItemSeverity : byte
{
    Error
}
