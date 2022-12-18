namespace sernick.CodeGeneration.RegisterAllocation;

using Compiler.Function;
using ControlFlowGraph.Analysis;
using ControlFlowGraph.CodeTree;

public sealed class SpillsAllocator
{
    private readonly IReadOnlyList<HardwareRegister> _registersReserve;
    private readonly InstructionCovering _instructionCovering;

    public SpillsAllocator(IReadOnlyList<HardwareRegister> registersReserve, InstructionCovering instructionCovering)
    {
        _registersReserve = registersReserve;
        _instructionCovering = instructionCovering;
    }

    /// <summary>
    ///     Creates new asm.
    ///     For each instruction that uses unallocated registers it inserts memory reads from stack before it.
    ///     For each instruction that defines unallocated registers it inserts memory writes to stack after it.
    ///     It replaces unallocated registers with hardware registers from reserve.
    /// </summary>
    /// <param name="asm">
    ///     Assemble representing code for a function.
    /// </param>
    /// <param name="functionContext">
    ///     FunctionContext object which generated <paramref name="asm"/>.
    /// </param>
    /// <param name="allocation">
    ///     An incomplete hardware registers allocation for <paramref name="asm"/>.
    ///     This allocation should not use hardware registers from <c>registersReserve</c>.
    /// </param>
    /// <returns>
    ///     A pair of new assembly and new complete allocation.
    /// </returns>
    public (IReadOnlyList<IAsmable>, IReadOnlyDictionary<Register, HardwareRegister>) Process(
        IEnumerable<IAsmable> asm,
        IFunctionContext functionContext,
        IReadOnlyDictionary<Register, HardwareRegister?> allocation)
    {
        // Assign to each unallocated register a new variable location on stack.
        var spillsLocation = allocation
            .Where(entry => entry.Value == null)
            .ToDictionary(entry => entry.Key, _ => functionContext.AllocateStackFrameSlot());

        IEnumerable<IAsmable> HandleSpill(IAsmable asmable)
        {
            if (asmable is not IInstruction instruction)
            {
                return new[] { asmable };
            }

            var usedRegisters = AssignReservedRegisters(instruction.RegistersUsed.Where(spillsLocation.ContainsKey));
            var definedRegisters = AssignReservedRegisters(instruction.RegistersDefined.Where(spillsLocation.ContainsKey));

            // Add read instructions from variable locations to a reserved registers
            var spilledInstructions = usedRegisters.SelectMany(tuple =>
            {
                var (spilledRegister, reservedRegister) = tuple;
                var location = spillsLocation[spilledRegister];
                var readCodeTree = new RegisterWrite(reservedRegister, location.GenerateRead());
                return _instructionCovering.Cover(readCodeTree);
            });

            // Modify instruction to use reserved registers and add it to the assembly
            var newInstruction = instruction.ReplaceRegisters(defines: definedRegisters, uses: usedRegisters);
            spilledInstructions = spilledInstructions.Append(newInstruction);

            // Add write instructions from reserved registers to variable locations
            spilledInstructions = spilledInstructions.Concat(definedRegisters.SelectMany(tuple =>
            {
                var (spilledRegister, reservedRegister) = tuple;
                var location = spillsLocation[spilledRegister];
                var writeCodeTree = location.GenerateWrite(new RegisterRead(reservedRegister));
                return _instructionCovering.Cover(writeCodeTree);
            }));

            return spilledInstructions;
        }

        // Create new register allocation by removing unallocated registers from allocation
        // and adding entries for reserved registers.
        var newAllocation = allocation
            .Where(entry => entry.Value != null)
            .ToDictionary(entry => entry.Key, entry => entry.Value!);
        foreach (var hardwareRegister in _registersReserve)
        {
            newAllocation.Add(hardwareRegister, hardwareRegister);
        }

        return (asm.SelectMany(HandleSpill).ToList(), newAllocation);
    }

    private Dictionary<Register, Register> AssignReservedRegisters(IEnumerable<Register> registers)
    {
        var registerList = registers.ToList();
        if (registerList.Count > _registersReserve.Count)
        {
            throw new Exception(
                $"Not enough reserved registers, required {registerList.Count}, but got {_registersReserve.Count}");
        }

        return registerList.Zip(_registersReserve.ToList<Register>())
            .ToDictionary(pair => pair.First, pair => pair.Second);
    }
}
