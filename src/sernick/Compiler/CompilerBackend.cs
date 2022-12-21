namespace sernick.Compiler;

using System.Collections.Immutable;
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
    /// <param name="filename">Filename with ".ser" that is being compiled</param>
    /// <param name="programInfo">Result of frontend phase</param>
    /// <returns>Filename of output binary</returns>
    /// <exception cref="AssemblingException"></exception>
    /// <exception cref="CompilationException"></exception>
    public static string Process(string filename, CompilerFrontendResult programInfo)
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

        var maxDepth = functionContextMap.Implementations.Values.Max(context => context.Depth);
        var displayTable = new DisplayTable(maxDepth + 1);

        var asm = "section .text".Enumerate()
            .Append("extern scanf")
            .Append("extern printf")
            .Append("global main")
            .Concat(functionCodeTreeMap
                .SelectMany((funcDef, codeTree) =>
                {
                    IReadOnlyList<IAsmable> asm = functionContextMap[funcDef].Label.Enumerate()
                        .Concat(linearizator.Linearize(codeTree))
                        .ToList();
                    var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(asm);
                    var regAllocation = regAllocator.Process(interferenceGraph, copyGraph);
                    IReadOnlyDictionary<Register, HardwareRegister> completeRegAllocation;

                    if (regAllocation.Values.Any(reg => reg is null))
                    {
                        regAllocation = spilledRegAllocator.Process(interferenceGraph, copyGraph);
                        (asm, completeRegAllocation) = spillsRegAllocator.Process(asm, functionContextMap[funcDef], regAllocation);
                        foreach (var asmable in asm)
                        {
                            try
                            {
                                asmable.ToAsm(completeRegAllocation);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(asmable);
                                Console.WriteLine(e);
                                throw;
                            }
                        }
                    }
                    else
                    {
                        completeRegAllocation = regAllocation!;
                    }

                    return asm.Select(asmable => asmable.ToAsm(completeRegAllocation));
                }))
            .Append(displayTable.ToAsm(ImmutableDictionary<Register, HardwareRegister>.Empty));

        var asmFilename = Path.ChangeExtension(filename, ".asm");
        File.WriteAllText(asmFilename, string.Join(Environment.NewLine, asm));

        var oFilename = Path.ChangeExtension(filename, ".o");
        var (errors, _) = "nasm".RunProcess($"-f elf64 -o {oFilename} {asmFilename}");

        if (errors.Length > 0)
        {
            throw new AssemblingException(errors);
        }

        var outFilename = Path.ChangeExtension(filename, ".out");
        (errors, _) = "gcc".RunProcess($"-no-pie -o {outFilename} {oFilename}");

        if (errors.Length > 0)
        {
            throw new LinkingException(errors);
        }

        return outFilename;
    }

    private static readonly IReadOnlyList<HardwareRegister> allRegisters = typeof(HardwareRegister)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(HardwareRegister))
        .Select(f => (HardwareRegister)f.GetValue(null)!)
        .ToList();

    private static readonly HardwareRegister[] spillsRegisters = { HardwareRegister.R10, HardwareRegister.R11 };

    private static readonly IReadOnlyList<HardwareRegister> reducedRegisters = allRegisters.Except(spillsRegisters).ToList();
}
