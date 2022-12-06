namespace sernick.CodeGeneration.LivenessAnalysis;

using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;

public static class LivenessAnalyzer
{
    public static (Graph interferenceGraph, Graph copyGraph) Process(IEnumerable<IAsmable> instructionList)
    {
        throw new NotImplementedException();
    }
}
