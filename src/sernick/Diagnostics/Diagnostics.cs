namespace sernick.Diagnostics;

public sealed class Diagnostics : IDiagnostics
{
    private readonly List<IDiagnosticItem> _diagnostics;
    public bool DidErrorOccur { get; private set; }
    public IReadOnlyList<IDiagnosticItem> DiagnosticItems => _diagnostics;

    public Diagnostics()
    {
        _diagnostics = new List<IDiagnosticItem>();
    }

    public void Report(IDiagnosticItem diagnosticItem)
    {
        _diagnostics.Add(diagnosticItem);
        if (diagnosticItem.Severity == DiagnosticItemSeverity.Error)
        {
            DidErrorOccur = true;
        }
    }
}
