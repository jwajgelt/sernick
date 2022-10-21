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

var filename = args[0];

// scan and process file
var file = filename.ReadFile();
IDiagnostics diagnostics = new Diagnostics();
CompilerFrontend.Process(file, diagnostics);

// log all diagnostics and exit
foreach (var diagnosticItem in diagnostics.DiagnosticItems)
{
    Console.Error.WriteLine(diagnosticItem.ToString());
}

Environment.Exit(diagnostics.DidErrorOccur ? 1 : 0);
