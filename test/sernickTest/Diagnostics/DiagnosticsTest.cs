namespace sernickTest.Diagnostics;

using sernick.Diagnostics;

public class DiagnosticsTest
{
    [Fact]
    public void DiagnosticItemsAreEmptyWhenCreated()
    {
        var diagnostics = new Diagnostics();

        var items = diagnostics.DiagnosticItems;

        Assert.Empty(items);
    }

    [Fact]
    public void DiagnosticItemsAreSavedWhenReported()
    {
        var diagnostics = new Diagnostics();
        var item1 = new DiagnosticItemFake(DiagnosticItemSeverity.Error);
        var item2 = new DiagnosticItemFake(DiagnosticItemSeverity.Info);

        diagnostics.Report(item1);
        diagnostics.Report(item2);

        Assert.Contains(item1, diagnostics.DiagnosticItems);
        Assert.Contains(item2, diagnostics.DiagnosticItems);
    }

    [Fact]
    public void DidErrorOccurFlagIsUpdated()
    {
        var diagnostics = new Diagnostics();
        var item1 = new DiagnosticItemFake(DiagnosticItemSeverity.Info);
        var item2 = new DiagnosticItemFake(DiagnosticItemSeverity.Error);

        var flag0 = diagnostics.DidErrorOccur;
        diagnostics.Report(item1);
        var flag1 = diagnostics.DidErrorOccur;
        diagnostics.Report(item2);
        var flag2 = diagnostics.DidErrorOccur;

        Assert.False(flag0);
        Assert.False(flag1);
        Assert.True(flag2);
    }
}
