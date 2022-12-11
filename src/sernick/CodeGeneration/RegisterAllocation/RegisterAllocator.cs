namespace sernick.CodeGeneration.RegisterAllocation;
using ControlFlowGraph.CodeTree;
using Utility;
using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;
using MutableGraph = IDictionary<ControlFlowGraph.CodeTree.Register, ICollection<ControlFlowGraph.CodeTree.Register>>;

public sealed class RegisterAllocator
{
    private readonly ISet<HardwareRegister> _hardwareRegisters;

    private bool IsHardwareRegister(Register register) => _hardwareRegisters.Contains(register);

    public RegisterAllocator(IEnumerable<HardwareRegister> hardwareRegisters)
    {
        _hardwareRegisters = hardwareRegisters.ToHashSet();
    }

    public IReadOnlyDictionary<Register, HardwareRegister?> Process(Graph interferenceGraph, Graph copyGraph)
    {
        var mapping = new Dictionary<Register, HardwareRegister?>();
        // colors of Register's neighbours
        var neighboursRegisters = new Dictionary<Register, HashSet<HardwareRegister>>();
        var orderedRegisters = EnumerateRegisters(interferenceGraph);

        foreach (var register in orderedRegisters)
        {
            if (IsHardwareRegister(register))
            {
                mapping[register] = register as HardwareRegister;
            }

            else
            {
                var copies = copyGraph.GetValueOrDefault(register, Array.Empty<Register>());
                mapping[register] = GetOptimalRegister(register, interferenceGraph, mapping, copies, neighboursRegisters);
            }

            AddToNeighboursRegisters(interferenceGraph[register], mapping[register], neighboursRegisters);
        }

        return mapping;
    }

    private HardwareRegister? GetOptimalRegister(
        Register register,
        Graph interferenceGraph,
        IReadOnlyDictionary<Register, HardwareRegister?> mapping,
        IReadOnlyCollection<Register> copies,
        IDictionary<Register, HashSet<HardwareRegister>> neighboursRegisters)
    {
        var availableRegisters = GetAvailableRegisters(mapping, interferenceGraph, register);

        // if the copy has available Register, then assign it
        foreach (var copy in copies)
        {
            if (mapping.TryGetValue(copy, out var copyRegister) && copyRegister != null && availableRegisters.Contains(copyRegister))
            {
                return copyRegister;
            }
        }

        // otherwise assign Register not assigned to the neighbours of the copy
        foreach (var copy in copies)
        {
            var optimalRegisters = availableRegisters.Where(reg => !neighboursRegisters.GetOrAddEmpty(copy).Contains(reg));
            var optimalRegister = optimalRegisters.FirstOrDefault();
            if (optimalRegister != null)
            {
                return optimalRegister;
            }
        }

        // otherwise return any available register
        return availableRegisters.FirstOrDefault();
    }

    private IReadOnlyCollection<HardwareRegister> GetAvailableRegisters(
        IReadOnlyDictionary<Register, HardwareRegister?> mapping,
        Graph interferenceGraph,
        Register register)
    {
        var availableRegisters = new HashSet<HardwareRegister?>(_hardwareRegisters);
        foreach (var neighbour in interferenceGraph[register])
        {
            availableRegisters.Remove(mapping.GetValueOrDefault(neighbour));
        }

        return availableRegisters!;
    }

    private IEnumerable<Register> EnumerateRegisters(Graph graph)
    {
        var registers = new List<Register>();
        var mutableGraph = CopyGraph(graph);

        // add non HardwareRegisters
        var register = GetMinimalDegreeRegister(mutableGraph);
        for (; register != null; register = GetMinimalDegreeRegister(mutableGraph))
        {
            registers.Add(register);
            RemoveRegister(mutableGraph, register);
        }

        // add HardwareRegisters
        registers.AddRange(mutableGraph.Keys);

        registers.Reverse();
        return registers;
    }

    // doesn't return HardwareRegisters, so they are assigned at the beginning
    private Register? GetMinimalDegreeRegister(MutableGraph graph) => graph
        .Where(entry => !IsHardwareRegister(entry.Key))
        .Select(entry => entry as KeyValuePair<Register, ICollection<Register>>?)
        .DefaultIfEmpty()
        .MinBy(entry => entry?.Value.Count ?? 0)?.Key;

    private static void AddToNeighboursRegisters(IReadOnlyCollection<Register> neighbours, HardwareRegister? hardwareRegister,
        IDictionary<Register, HashSet<HardwareRegister>> neighboursRegisters)
    {
        if (hardwareRegister == null)
        {
            return;
        }

        foreach (var neighbour in neighbours)
        {
            neighboursRegisters.GetOrAddEmpty(neighbour).Add(hardwareRegister);
        }
    }

    private static void RemoveRegister(MutableGraph graph, Register register)
    {
        var neighbours = graph[register];
        foreach (var neighbour in neighbours)
        {
            graph[neighbour].Remove(register);
        }

        graph.Remove(register);
    }

    private static MutableGraph CopyGraph(Graph graph) => graph.ToDictionary
    (
        entry => entry.Key,
        entry => (ICollection<Register>)entry.Value.ToHashSet()
    );
}
