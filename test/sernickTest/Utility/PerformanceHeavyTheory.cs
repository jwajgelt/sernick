namespace sernickTest.Utility;

/// <summary>
/// Marks a test as a Theory but automatically skips this test if
/// environment variable `SkipPerformanceHeavy` is set to `true`.
///
/// Use this attribute to mark tests which take a long time to execute
/// to avoid reaching time limits on Github Workflows.
/// </summary>
public sealed class PerformanceHeavyTheory : TheoryAttribute
{
    public PerformanceHeavyTheory()
    {
        var skipHeavy = Environment.GetEnvironmentVariable("SkipPerformanceHeavy");
        if (skipHeavy is "true")
        {
            Skip = "Ignore performance heavy tests.";
        }
    }
}
