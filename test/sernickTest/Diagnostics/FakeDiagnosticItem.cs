namespace sernickTest.Diagnostics;

using sernick.Diagnostics;

public record FakeDiagnosticItem(DiagnosticItemSeverity Severity) : IDiagnosticItem
{
}
