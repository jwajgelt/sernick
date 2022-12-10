namespace sernick.CodeGeneration.LivenessAnalysis;

using ControlFlowGraph.CodeTree;
using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;

public static class LivenessAnalyzer
{
    public static (Graph interferenceGraph, Graph copyGraph) Process(IEnumerable<IAsmable> instructions)
    {
        var instructionList = instructions.ToList();

        var instructionCount = instructionList.Count;

        var registers = instructionList.SelectMany(asmable => asmable switch
        {
            IInstruction instruction => instruction.RegistersDefined.Union(instruction.RegistersUsed),
            _ => new Register[] { }
        }).ToHashSet();

        var livenessInformation = instructionList
            .Select(
                asmable => asmable is not IInstruction instruction
                    ? new LivenessAtInstruction(new HashSet<Register>(), new HashSet<Register>())
                    : new LivenessAtInstruction(instruction.RegistersUsed.ToHashSet(), instruction.RegistersDefined.ToHashSet()))
            .ToList();

        var labelLocations = instructionList
            .SelectMany(
                (asmable, i) =>
                {
                    return asmable switch
                    {
                        Label label => new[] { (label, i) },
                        _ => new (Label, int)[] { },
                    };
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
                            return i + 1 < instructionCount ? new List<int> { i + 1 } : new List<int>();
                        case IInstruction instruction:
                            var result = new List<int>();
                            if (instruction.PossibleFollow && i + 1 < instructionCount)
                            {
                                result.Add(i + 1);
                            }

                            if (instruction.PossibleJump != null)
                            {
                                result.Add(labelLocations[instruction.PossibleJump]);
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
            var definedRegisters = instruction.RegistersDefined.ToList();
            foreach (var x in definedRegisters)
            {
                foreach (var y in liveRegisters)
                {
                    if (x == y)
                    {
                        continue;
                    }

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

        foreach (var x in registers)
        {
            foreach (var y in interferenceGraph[x])
            {
                copyGraph[x].Remove(y);
                copyGraph[y].Remove(x);
            }
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
