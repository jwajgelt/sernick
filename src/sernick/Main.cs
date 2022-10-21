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
    CompilerFrontend.Process(file, diagnostics);
}
catch (CompilationException _)
{
    Console.Error.WriteLine("Compilation failed.");
    success = false;
}

// log the diagnostics
foreach (var diagnosticItem in diagnostics.DiagnosticItems)
{
    Console.Error.WriteLine(diagnosticItem.ToString());
}

// exit
Environment.Exit(success ? 0 : 1);
