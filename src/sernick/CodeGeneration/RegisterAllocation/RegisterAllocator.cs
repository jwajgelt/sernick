namespace sernick.CodeGeneration.RegisterAllocation;

using ControlFlowGraph.CodeTree;
using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;
using MutableGraph = IDictionary<ControlFlowGraph.CodeTree.Register, ICollection<ControlFlowGraph.CodeTree.Register>>;

public sealed class RegisterAllocator
{
    private readonly IEnumerable<HardwareRegister> _hardwareRegisters;

    private bool IsHardwareRegister(Register register) => _hardwareRegisters.Contains(register);

    public RegisterAllocator(IEnumerable<HardwareRegister> hardwareRegisters)
    {
        _hardwareRegisters = hardwareRegisters;
    }

    public IReadOnlyDictionary<Register, HardwareRegister?> Process(Graph interferenceGraph, Graph copyGraph)
    {
        var mapping = new Dictionary<Register, HardwareRegister?>();
        var orderedRegisters = EnumerateRegisters(interferenceGraph);
        foreach (var register in orderedRegisters)
        {
            if (IsHardwareRegister(register))
            {
                mapping[register] = register as HardwareRegister;
            }
            
            else
            {
                mapping[register] = GetAvailableRegister(mapping, interferenceGraph, register);
            }
        }

        return mapping;
    }

    private HardwareRegister? GetAvailableRegister(
        IReadOnlyDictionary<Register, HardwareRegister?> mapping, 
        Graph interferenceGraph,
        Register register)
    {
        var availableRegisters = new HashSet<HardwareRegister?>(_hardwareRegisters);
        foreach (var neighbour in interferenceGraph[register])
        {
            availableRegisters.Remove(mapping.GetValueOrDefault(neighbour));
        }
        
        return availableRegisters.FirstOrDefault();
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
    private Register? GetMinimalDegreeRegister(MutableGraph graph)
    {
        Register? minRegister = null;
        var minDegree = 0;
        foreach (var (register, neighbours) in graph)
        {
            if (IsHardwareRegister(register))
            {
                continue;
            }

            var degree = neighbours.Count;

            if (minRegister == null || minDegree > degree)
            {
                minRegister = register;
                minDegree = degree;
            }
        }

        return minRegister;
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
        entry => (ICollection<Register>)new List<Register>(entry.Value)
    );
}
