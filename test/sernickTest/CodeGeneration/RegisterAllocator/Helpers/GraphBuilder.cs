namespace sernickTest.CodeGeneration.RegisterAllocator.Helpers;

using ControlFlowGraph;
using sernick.ControlFlowGraph.CodeTree;
using sernick.Utility;
using Graph = IReadOnlyDictionary<sernick.ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<sernick.ControlFlowGraph.CodeTree.Register>>;

public class GraphBuilder
{
    private readonly List<(Register, Register)> _edges = new();

    public static GraphBuilder Graph() => new GraphBuilder();

    public GraphBuilder Edge(Register register1, Register register2)
    {
        _edges.Add((register1, register2));
        return this;
    }

    public GraphBuilder Edge(int id1, int id2)
    {
        _edges.Add(((FakeRegister)id1, (FakeRegister)id2));
        return this;
    }

    public GraphBuilder Edge(int id1, char id2)
    {
        _edges.Add(((FakeRegister)id1, (FakeHardwareRegister)id2));
        return this;
    }

    public GraphBuilder Edge(char id1, char id2)
    {
        _edges.Add(((FakeHardwareRegister)id1, (FakeHardwareRegister)id2));
        return this;
    }

    public Graph Build()
    {
        var graph = new Dictionary<Register, List<Register>>();
        foreach (var (a, b) in _edges)
        {
            graph.GetOrAddEmpty(a).Add(b);
            graph.GetOrAddEmpty(b).Add(a);
        }

        return graph.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyCollection<Register>)kv.Value);
    }

    public static Graph Clique(int size)
    {
        var registers = Enumerable.Range(1, size).Select(id => (Register)new FakeRegister(id)).ToList();
        return registers.ToDictionary(register => register,
            register => (IReadOnlyCollection<Register>)registers.Where(neighbour => neighbour != register).ToList());
    }
}
