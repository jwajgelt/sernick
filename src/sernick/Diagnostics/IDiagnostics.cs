namespace sernick.Diagnostics;

public interface IDiagnostics
{
    IEnumerable<IDiagnosticItem> DiagnosticItems { get; }
    bool DidErrorOccur { get; }
    void Report(IDiagnosticItem diagnosticItem);
}

public interface IDiagnosticItem
{
    string ToString();

    DiagnosticItemSeverity Severity { get; }
}

public enum DiagnosticItemSeverity : byte
{
    Info,
    Error
}
