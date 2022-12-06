namespace sernick.CodeGeneration.RegisterAllocation;

using ControlFlowGraph.CodeTree;
using Graph = IReadOnlyDictionary<ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<ControlFlowGraph.CodeTree.Register>>;

public class RegisterAllocator
{
    public RegisterAllocator(IReadOnlyList<HardwareRegister> hardwareRegisters)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyDictionary<Register, HardwareRegister> Process(Graph interferenceGraph, Graph copyGraph)
    {
        throw new NotImplementedException();
    }
}
