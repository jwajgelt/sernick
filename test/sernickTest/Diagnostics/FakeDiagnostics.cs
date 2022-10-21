using sernick.Diagnostics;

namespace sernickTest.Diagnostics;

public class FakeDiagnostics : IDiagnostics

{
    public void Report(IDiagnosticItem diagnosticItem)
    {
        _items.Add(diagnosticItem);
    }

    private readonly List<IDiagnosticItem> _items = new();
    public IEnumerable<IDiagnosticItem> Items => _items.AsReadOnly();
}
