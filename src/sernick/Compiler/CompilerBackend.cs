namespace sernick.Compiler;

using System.Diagnostics;
using Ast.Analysis.ControlFlowGraph;
using Ast.Analysis.FunctionContextMap;
using CodeGeneration.LivenessAnalysis;
using CodeGeneration.RegisterAllocation;
using ControlFlowGraph.Analysis;
using ControlFlowGraph.CodeTree;
using Function;
using Instruction;
using Utility;

public static class CompilerBackend
{
    public static string Process(string programName, CompilerFrontendResult programInfo)
    {
        var (astRoot, nameResolution, typeCheckingResult, callGraph, variableAccessMap) = programInfo;

        var functionContextMap = FunctionContextMapProcessor.Process(astRoot, nameResolution, new FunctionFactory(LabelGenerator.Generate));
        var functionCodeTreeMap = FunctionCodeTreeMapGenerator.Process(astRoot,
            root =>
                ControlFlowAnalyzer.UnravelControlFlow(root, nameResolution, functionContextMap, callGraph, variableAccessMap, typeCheckingResult, SideEffectsAnalyzer.PullOutSideEffects));

        var linearizator = new Linearizator(new InstructionCovering(SernickInstructionSet.Rules));
        var regAllocator = new RegisterAllocator(HardwareRegister.RAX.Enumerate()); // TODO: hardware regs

        var asm = functionCodeTreeMap
            .SelectMany((funcDef, codeTree) =>
            {
                var asm = linearizator.Linearize(codeTree).ToList();
                var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(asm);
                var regAllocation = regAllocator.Process(interferenceGraph, copyGraph);
                // TODO: integrate spills
                return functionContextMap[funcDef].Label.Enumerate().Concat(asm)
                    .Select(asmable => asmable.ToAsm(regAllocation));
            });

        File.WriteAllText($"{programName}.asm", string.Join(Environment.NewLine, asm));

        var (errors, _) = RunProcess("nasm", $"-f elf64 -o {programName}.o {programName}.asm");

        if (errors.Length > 0)
        {
            throw new CompilationException();
        }

        (errors, _) = RunProcess("gcc", $"-no-pie -o {programName}.out {programName}.o");

        if (errors.Length > 0)
        {
            throw new CompilationException();
        }

        return $"{programName}.out";
    }

    private static (string stderr, string stdout) RunProcess(string cmd, string arguments = "")
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
}
