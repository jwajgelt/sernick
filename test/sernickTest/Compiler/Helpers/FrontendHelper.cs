namespace sernickTest.Compiler.Helpers;

using Diagnostics;
using sernick.Compiler;
using sernick.Utility;

public static class FrontendHelper
{
    public static FakeDiagnostics Compile(this string fileName)
    {
        var input = fileName.ReadFile();
        var diagnostics = new FakeDiagnostics();

        try
        {
            CompilerFrontend.Process(input, diagnostics);
        }
        catch
        {
            // exceptions ignored, so the diagnostics can be analyzed in the tests
        }

        return diagnostics;
    }
}
