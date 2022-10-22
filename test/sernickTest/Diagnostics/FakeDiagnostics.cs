namespace sernickTest.Diagnostics;

using sernick.Diagnostics;

public class FakeDiagnostics : IDiagnostics
{
    private readonly List<IDiagnosticItem> _items = new();
    public void Report(IDiagnosticItem diagnosticItem) => _items.Add(diagnosticItem);
    public IEnumerable<IDiagnosticItem> Items => _items;
}
