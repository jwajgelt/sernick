namespace sernick.Diagnostics;

public interface IDiagnostics
{
    IEnumerable<IDiagnosticItem> DiagnosticItems { get; }
    bool DidErrorOccur { get; }
    void Report(IDiagnosticItem diagnosticItem);
}

public interface IDiagnosticItem : IEquatable<IDiagnosticItem>
{
    string ToString();

    DiagnosticItemSeverity Severity { get; }

    bool IEquatable<IDiagnosticItem>.Equals(IDiagnosticItem? other) =>
        other is not null && GetType() == other.GetType() && ToString() == other.ToString();
}

public enum DiagnosticItemSeverity : byte
{
    Info,
    Error
}
