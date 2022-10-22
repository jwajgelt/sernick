namespace sernick.Diagnostics;

public sealed class Diagnostics : IDiagnostics
{
    public bool DidErrorOccur { get; private set; }
    public IEnumerable<IDiagnosticItem> DiagnosticItems => _diagnostics;

    private readonly List<IDiagnosticItem> _diagnostics = new();

    public void Report(IDiagnosticItem diagnosticItem)
    {
        _diagnostics.Add(diagnosticItem);
        if (diagnosticItem.Severity == DiagnosticItemSeverity.Error)
        {
            DidErrorOccur = true;
        }
    }
}
