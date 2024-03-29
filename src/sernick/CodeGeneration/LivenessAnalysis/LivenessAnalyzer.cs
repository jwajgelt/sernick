namespace sernick.CodeGeneration.LivenessAnalysis;

using ControlFlowGraph.CodeTree;
using Utility;
using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;

public static class LivenessAnalyzer
{
    public static (Graph interferenceGraph, Graph copyGraph) Process(IEnumerable<IAsmable> instructions)
    {
        var instructionList = instructions.ToList();

        var instructionCount = instructionList.Count;

        var registers = instructionList.OfType<IInstruction>().SelectMany(instruction =>
            instruction.RegistersDefined.Union(instruction.RegistersUsed)
        ).ToHashSet();

        var livenessInformation = instructionList
            .Select(
                asmable => asmable is not IInstruction instruction
                    ? new LivenessAtInstruction(new HashSet<Register>(), new HashSet<Register>())
                    : new LivenessAtInstruction(instruction.RegistersUsed.ToHashSet(), instruction.RegistersDefined.ToHashSet()))
            .ToList();

        var labelLocations = instructionList
            .SelectMany(
                // this essentially filters out non-label elements,
                // but we can't use `OfType` here
                // since we want the original indices
                (asmable, i) =>
                    asmable switch
                    {
                        Label label => (label, i).Enumerate(),
                        _ => Enumerable.Empty<(Label, int)>(),
                    }
            )
            .ToDictionary
            (
                p => p.Item1,
                p => p.Item2
            );

        var possibleNextInstructions = instructionList
            .Select(
                (asmable, i) =>
                {
                    switch (asmable)
                    {
                        case Label:
                            return i + 1 < instructionCount ? (i + 1).Enumerate() : Enumerable.Empty<int>();
                        case IInstruction instruction:
                            var result = new List<int>();
                            if (instruction.PossibleFollow && i + 1 < instructionCount)
                            {
                                result.Add(i + 1);
                            }

                            if (instruction.PossibleJump != null &&
                                labelLocations.TryGetValue(instruction.PossibleJump, out var possibleJump))
                            {
                                result.Add(possibleJump);
                            }

                            return result;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(asmable), asmable, null);
                    }
                })
            .ToList();

        var possiblePreviousInstructions = possibleNextInstructions.Select(_ => new List<int>()).ToList();
        foreach (var current in Enumerable.Range(0, instructionCount))
        {
            foreach (var next in possibleNextInstructions[current])
            {
                possiblePreviousInstructions[next].Add(current);
            }
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var current in Enumerable.Range(0, instructionCount))
            {
                changed = possibleNextInstructions[current]
                    .SelectMany(next => livenessInformation[next].LiveAtEntry)
                    .Aggregate
                    (
                        changed,
                        (changedBefore, register) =>
                            livenessInformation[current].LiveAtExit.Add(register) || changedBefore
                    );

                foreach (var register in livenessInformation[current].LiveAtExit)
                {
                    var shouldAdd = instructionList[current] switch
                    {
                        IInstruction instruction => !instruction.RegistersDefined.Contains(register),
                        _ => true
                    };
                    if (shouldAdd)
                    {
                        changed = livenessInformation[current].LiveAtEntry.Add(register) || changed;
                    }
                }
            }
        }

        var interferenceGraph = registers.ToDictionary(register => register, _ => new HashSet<Register>());
        var copyGraph = registers.ToDictionary(register => register, _ => new HashSet<Register>());

        foreach (var current in Enumerable.Range(0, instructionCount))
        {
            if (instructionList[current] is not IInstruction instruction)
            {
                continue;
            }

            var liveRegisters = livenessInformation[current].LiveAtExit;
            var definedRegisters = instruction.RegistersDefined;
            foreach (var x in definedRegisters)
            {
                foreach (var y in liveRegisters.Where(y => y != x))
                {
                    if (instruction.IsCopy && instruction.RegistersUsed.Contains(y))
                    {
                        copyGraph[x].Add(y);
                        copyGraph[y].Add(x);
                        continue;
                    }

                    interferenceGraph[x].Add(y);
                    interferenceGraph[y].Add(x);
                }
            }
        }

        if (interferenceGraph.TryGetValue(HardwareRegister.RBP, out var rbpInterference))
        {
            foreach (var x in registers)
            {
                interferenceGraph[x].Add(HardwareRegister.RBP);
            }

            rbpInterference.UnionWith(registers);
            rbpInterference.Remove(HardwareRegister.RBP);
        }

        foreach (var x in registers)
        {
            copyGraph[x].ExceptWith(interferenceGraph[x]);
        }

        return
        (
            interferenceGraph.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyCollection<Register>)kv.Value
            ),
            copyGraph.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyCollection<Register>)kv.Value
            )
        );
    }

    private record LivenessAtInstruction(
        HashSet<Register> LiveAtEntry,
        HashSet<Register> LiveAtExit
    );
}
