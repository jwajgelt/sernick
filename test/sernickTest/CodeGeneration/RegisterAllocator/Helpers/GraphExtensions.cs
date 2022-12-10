namespace sernickTest.CodeGeneration.RegisterAllocator.Helpers;

using sernick.ControlFlowGraph.CodeTree;
using Graph = IReadOnlyDictionary<sernick.ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<sernick.ControlFlowGraph.CodeTree.Register>>;

public static class GraphExtensions
{
    internal static IEnumerable<(Register from, Register to)> Edges(this Graph graph) =>
        graph.SelectMany(kv => kv.Value.Select(register => (from: kv.Key, to: register)));
}
