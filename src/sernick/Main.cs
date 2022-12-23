using System.Runtime.CompilerServices;
using sernick.Compiler;
using sernick.Diagnostics;
using sernick.Utility;

// Usage: ./sernick.exe program.ser [--execute]
// --execute flag compiles and runs the compiled program immediately

[assembly: InternalsVisibleTo("sernickTest")]

// check if filename was provided
if (args.Length == 0)
{
    Console.Error.WriteLine("Fatal error: No arguments provided.");
    Environment.Exit(1);
}

// scan the file
var filename = args[0];
var file = filename.ReadFile();

// try to process the file
var success = true;
IDiagnostics diagnostics = new Diagnostics();
try
{
    var frontendResult = CompilerFrontend.Process(file, diagnostics);
    var outputFilename = CompilerBackend.Process(filename, frontendResult);
    Console.WriteLine(outputFilename);

    if (args.Length > 1 && args[1] == "--execute")
    {
        Console.WriteLine("Executing...");
        var (errors, output) = outputFilename.RunProcess();
        Console.Error.WriteLine(errors);
        Console.WriteLine(output);
    }
}
catch (CompilationException e)
{
    Console.Error.WriteLine("Compilation failed.");
    Console.Error.WriteLine(e.Message);
    success = false;
}

// log the diagnostics
foreach (var diagnosticItem in diagnostics.DiagnosticItems)
{
    Console.Error.WriteLine(diagnosticItem);
}

// exit
Environment.Exit(success ? 0 : 1);
