namespace sernick.Compiler;

using System.Reflection;
using Ast.Analysis.ControlFlowGraph;
using Ast.Analysis.FunctionContextMap;
using CodeGeneration;
using CodeGeneration.LivenessAnalysis;
using CodeGeneration.RegisterAllocation;
using ControlFlowGraph.Analysis;
using ControlFlowGraph.CodeTree;
using Function;
using Instruction;
using Utility;

public static class CompilerBackend
{
    /// <summary>
    /// Backend phase.
    /// </summary>
    /// <param name="programName">Filename that is being compiled, without ".ser" extension</param>
    /// <param name="programInfo">Result of frontend phase</param>
    /// <returns>Filename of output binary</returns>
    /// <exception cref="AssemblingException"></exception>
    /// <exception cref="CompilationException"></exception>
    public static string Process(string programName, CompilerFrontendResult programInfo)
    {
        var (astRoot, nameResolution, typeCheckingResult, callGraph, variableAccessMap) = programInfo;

        var functionContextMap = FunctionContextMapProcessor.Process(astRoot, nameResolution,
            FunctionDistinctionNumberProcessor.Process(astRoot), new FunctionFactory(LabelGenerator.Generate));
        var functionCodeTreeMap = FunctionCodeTreeMapGenerator.Process(astRoot,
            root =>
                ControlFlowAnalyzer.UnravelControlFlow(root, nameResolution, functionContextMap, callGraph, variableAccessMap, typeCheckingResult, SideEffectsAnalyzer.PullOutSideEffects));

        var instructionCovering = new InstructionCovering(SernickInstructionSet.Rules);
        var linearizator = new Linearizator(instructionCovering);
        var regAllocator = new RegisterAllocator(allRegisters);
        var spilledRegAllocator = new RegisterAllocator(reducedRegisters);
        var spillsRegAllocator = new SpillsAllocator(spillsRegisters, instructionCovering);

        var asm = functionCodeTreeMap
            .SelectMany((funcDef, codeTree) =>
            {
                IReadOnlyList<IAsmable> asm = linearizator.Linearize(codeTree).ToList();
                var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(asm);
                var regAllocation = regAllocator.Process(interferenceGraph, copyGraph);
                IReadOnlyDictionary<Register, HardwareRegister> completeRegAllocation;

                if (regAllocation.Values.Any(reg => reg is null))
                {
                    regAllocation = spilledRegAllocator.Process(interferenceGraph, copyGraph);
                    (asm, completeRegAllocation) = spillsRegAllocator.Process(asm, functionContextMap[funcDef], regAllocation);
                }
                else
                {
                    completeRegAllocation = regAllocation!;
                }

                return functionContextMap[funcDef].Label.Enumerate().Concat(asm)
                    .Select(asmable => asmable.ToAsm(completeRegAllocation));
            });

        File.WriteAllText($"{programName}.asm", string.Join(Environment.NewLine, asm));

        var (errors, _) = "nasm".RunProcess($"-f elf64 -o {programName}.o {programName}.asm");

        if (errors.Length > 0)
        {
            throw new AssemblingException(errors);
        }

        (errors, _) = "gcc".RunProcess($"-no-pie -o {programName}.out {programName}.o");

        if (errors.Length > 0)
        {
            throw new CompilationException(errors);
        }

        return $"{programName}.out";
    }

    private static readonly IReadOnlyList<HardwareRegister> allRegisters = typeof(HardwareRegister)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(HardwareRegister))
        .Select(f => (HardwareRegister)f.GetValue(null)!)
        .ToList();

    private static readonly HardwareRegister[] spillsRegisters = { HardwareRegister.R10, HardwareRegister.R11 };

    private static readonly IReadOnlyList<HardwareRegister> reducedRegisters = allRegisters.Except(spillsRegisters).ToList();
}
