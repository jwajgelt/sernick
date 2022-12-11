namespace sernickTest.CodeGeneration.RegisterAllocator.Helpers;

using ControlFlowGraph;
using sernick.ControlFlowGraph.CodeTree;
using sernick.Utility;
using Graph = IReadOnlyDictionary<sernick.ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<sernick.ControlFlowGraph.CodeTree.Register>>;

public class GraphBuilder
{
    private readonly List<(Register, Register)> _edges = new();

    public static GraphBuilder Graph() => new GraphBuilder();

    public GraphBuilder Edge(FakeRegister id1, FakeRegister id2)
    {
        _edges.Add((id1, id2));
        return this;
    }

    public GraphBuilder Edge(FakeRegister id1, FakeHardwareRegister id2)
    {
        _edges.Add((id1, id2));
        return this;
    }

    public GraphBuilder Edge(FakeHardwareRegister id1, FakeHardwareRegister id2)
    {
        _edges.Add((id1, id2));
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
        var registers = Enumerable.Range(1, size).Select(id => new FakeRegister(id)).ToList<Register>();
        return registers.ToDictionary(register => register,
            register => (IReadOnlyCollection<Register>)registers.Where(neighbour => neighbour != register).ToList());
    }

    public static Graph StarGraph(int branches)
    {
        var builder = new GraphBuilder();
        foreach (var branch in Enumerable.Range(2, branches))
        {
            builder.Edge(1, branch);
        }

        return builder.Build();
    }
}
