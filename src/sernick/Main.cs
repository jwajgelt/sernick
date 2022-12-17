using System.Diagnostics;
using System.Runtime.CompilerServices;
using sernick.Compiler;
using sernick.Diagnostics;
using sernick.Utility;

[assembly: InternalsVisibleTo("sernickTest")]

const string ASM = @"section .text
extern printf
global main

main:
    push rbp
    mov rdi, message
    xor rax, rax
    call printf
    pop rbp
    mov rax, 0
    ret

section .data
    message: db 'Hello World', 10, 0";

Console.WriteLine("Assembling:");
Console.WriteLine(ASM);

await File.WriteAllTextAsync("main.asm", ASM);

(string stderr, string stdout) RunProcess(string cmd, string arguments = "")
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        }
    };

    process.Start();

    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();

    return (stderr, stdout);
}

var (errors, _) = RunProcess("nasm", "-f elf64 -o main.o main.asm");

if (errors.Length > 0)
{
    // handle errors
    Environment.Exit(1);
}

(errors, _) = RunProcess("gcc", "-no-pie -o main.out main.o");

if (errors.Length > 0)
{
    // handle errors
    Environment.Exit(1);
}

(errors, var output) = RunProcess("main.out");

if (errors.Length > 0)
{
    // handle errors
    Environment.Exit(1);
}

Console.WriteLine("Success:");
Console.WriteLine(output);

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
