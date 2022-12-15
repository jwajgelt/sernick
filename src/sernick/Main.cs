using System.Runtime.CompilerServices;
using sernick.Compiler;
using sernick.Diagnostics;
using sernick.Utility;

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
    var outputFilename = CompilerBackend.Process(filename.Split('.')[0], frontendResult);
    Console.WriteLine(outputFilename);
}
catch (CompilationException)
{
    Console.Error.WriteLine("Compilation failed.");
    success = false;
}

// log the diagnostics
foreach (var diagnosticItem in diagnostics.DiagnosticItems)
{
    Console.Error.WriteLine(diagnosticItem);
}

// exit
Environment.Exit(success ? 0 : 1);
