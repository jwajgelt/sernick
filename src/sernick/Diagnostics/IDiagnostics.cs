namespace sernick.Diagnostics;

public interface IDiagnostics
{
    public IReadOnlyList<IDiagnosticItem> DiagnosticItems { get; }
    public bool DidErrorOccur { get; }
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
