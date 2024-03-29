namespace sernickTest.Compiler.Helpers;

using Diagnostics;
using sernick.Compiler;
using sernick.Grammar.Syntax;
using sernick.Input;
using sernick.Input.String;
using sernick.Parser;
using sernick.Utility;

public static class FrontendHelper
{
    public static FakeDiagnostics Compile(this IInput input)
    {
        var diagnostics = new FakeDiagnostics();

        try
        {
            CompilerFrontend.Process(input, diagnostics);
        }
        catch (Exception e) when (e is CompilationException or ParsingException<Symbol>)
        {
            // compilation exceptions ignored, so the diagnostics can be analyzed in the tests
        }

        return diagnostics;
    }
    public static FakeDiagnostics CompileText(this string text)
    {
        var input = new StringInput(text);
        return input.Compile();
    }

    public static FakeDiagnostics CompileFile(this string fileName)
    {
        var input = fileName.ReadFile();
        return input.Compile();
    }
}
