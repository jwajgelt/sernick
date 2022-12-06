namespace sernick.CodeGeneration.LivenessAnalysis;

using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;

public class LivenessAnalyzer
{
    public static (Graph interferenceGraph, Graph copyGraph) Process(IReadOnlyList<IAsmable> instructionList)
    {
        throw new NotImplementedException();
    }
}
