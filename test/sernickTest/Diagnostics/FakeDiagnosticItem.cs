namespace sernickTest.Diagnostics;

using sernick.Diagnostics;

public record FakeDiagnosticItem(DiagnosticItemSeverity Severity) : IDiagnosticItem
{
    public bool Equals(IDiagnosticItem? other) => other is FakeDiagnosticItem && other.Severity == Severity && other.ToString() == ToString();
}
