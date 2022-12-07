namespace sernick.CodeGeneration.RegisterAllocation;

using ControlFlowGraph.CodeTree;
using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;

public sealed class RegisterAllocator
{
    public RegisterAllocator(IEnumerable<HardwareRegister> hardwareRegisters)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyDictionary<Register, HardwareRegister> Process(Graph interferenceGraph, Graph copyGraph)
    {
        throw new NotImplementedException();
    }
}
