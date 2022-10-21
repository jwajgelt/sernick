using sernick.Diagnostics;

namespace sernickTest.Diagnostics;

public class DiagnosticItemFake : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity { get; }

    public DiagnosticItemFake(DiagnosticItemSeverity severity)
    {
        Severity = severity;
    }

    public override string ToString()
    {
        return $"Diagnostic Item Fake of Severity {Severity}";
    }
}
