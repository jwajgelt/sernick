namespace sernick.CodeGeneration.RegisterAllocation;

using Compiler.Function;
using ControlFlowGraph.Analysis;
using ControlFlowGraph.CodeTree;

public class SpillsAllocator
{
    private IReadOnlyList<HardwareRegister> _registersReserve;

    public SpillsAllocator(IReadOnlyList<HardwareRegister> registersReserve)
    {
        _registersReserve = registersReserve;
    }

    public void Process(IFunctionContext functionContext,
        CodeTreeRoot codeTreeRoot,
        InstructionCovering instructionCovering,
        IReadOnlyDictionary<Register, HardwareRegister?> incompleteAllocation)
    {
        var spillsLocation = incompleteAllocation
            .Where(kv => kv.Value == null)
            .ToDictionary(kv => kv.Key, _ => functionContext.AllocateStackFrameSlot());

        IAsmable[] HandleSpill(IAsmable asmable)
        {
            if (asmable is not IInstruction instruction)
            {
                return new[] { asmable };
            }
        
            var spillsBefore = instruction.RegistersUsed.Where(spillsLocation.ContainsKey);
            var spillsAfter = instruction.RegistersDefined.Where(spillsLocation.ContainsKey);
        
            var readSpills = AssignReservedRegisters(spillsBefore).Select(tuple =>
            {
                var (spilledRegister, reservedRegister) = tuple;
                var location = spillsLocation[spilledRegister];
                var readNode = new RegisterWrite(reservedRegister, location.GenerateRead());
                return instructionCovering.Cover(new SingleExitNode(null, new[] { readNode }));
            });
        
            return new IAsmable[] { instruction };
        }
    }

    private IEnumerable<(Register, HardwareRegister)> AssignReservedRegisters(IEnumerable<Register> registers)
    {
        var registerList = registers.ToList();
        if (registerList.Count < _registersReserve.Count)
        {
            throw new Exception(
                $"Not enough reserved registers, required {registerList.Count}, but got {_registersReserve.Count}");
        }
        return registerList.Zip(_registersReserve);
    }
}
