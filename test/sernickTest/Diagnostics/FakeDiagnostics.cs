namespace sernickTest.Diagnostics;

using sernick.Diagnostics;

public class FakeDiagnostics : IDiagnostics
{
    private readonly List<IDiagnosticItem> _items = new();
    public IEnumerable<IDiagnosticItem> DiagnosticItems => _items;
    public bool DidErrorOccur => _items.Any();
    public void Report(IDiagnosticItem diagnosticItem) => _items.Add(diagnosticItem);
}
