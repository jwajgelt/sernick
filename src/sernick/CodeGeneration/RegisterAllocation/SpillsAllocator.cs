namespace sernick.CodeGeneration.RegisterAllocation;

using System.Collections.Immutable;
using Compiler.Function;
using ControlFlowGraph.Analysis;
using ControlFlowGraph.CodeTree;
using Utility;

public sealed class SpillsAllocator
{
    private readonly IReadOnlyList<HardwareRegister> _registersReserve;
    private readonly InstructionCovering _instructionCovering;

    public SpillsAllocator(IEnumerable<HardwareRegister> registersReserve, InstructionCovering instructionCovering)
    {
        _registersReserve = registersReserve.ToList();
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
                return asmable.Enumerate();
            }

            var usedRegisters = instruction.RegistersUsed.Where(spillsLocation.ContainsKey).ToList();
            var definedRegisters = instruction.RegistersDefined.Where(spillsLocation.ContainsKey).ToList();

            // Assign to each used register an unique hardware register from reserve pool.
            var usesAssigment = AssignReservedRegisters(usedRegisters);

            // Assign to each defined register an unique hardware register from reserve pool.
            // If register has an assigned hardware register in `usesAssigment` use the same hardware register.
            var definesAssigment = AssignReservedRegisters(definedRegisters, usesAssigment);

            // Combine two assignments.
            // Note that if `usesAssigment`, `definesAssigment` contain the same key
            // then it should get the same hardware register.
            var reserveAssigment = usesAssigment.JoinWith(definesAssigment);

            // Add read instructions from variable locations to a reserved registers.
            var spilledInstructions = usedRegisters.SelectMany(usedRegister =>
            {
                var reservedRegister = reserveAssigment[usedRegister];
                var location = spillsLocation[usedRegister];
                var readCodeTree = new RegisterWrite(reservedRegister, location.GenerateRead());
                return _instructionCovering.Cover(readCodeTree);
            });

            // Modify instruction to use reserved registers and add it to the assembly
            var newInstruction = instruction.MapRegisters(reserveAssigment);
            spilledInstructions = spilledInstructions.Append(newInstruction);

            // Add write instructions from reserved registers to variable locations
            spilledInstructions = spilledInstructions.Concat(definedRegisters.SelectMany(definedRegister =>
            {
                var reservedRegister = reserveAssigment[definedRegister];
                var location = spillsLocation[definedRegister];
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

    /// <summary>
    ///     Assigns to each register in <paramref name="registers"/>
    ///     an unique hardware register from <c>_registersReserve</c>.
    /// </summary>
    /// <exception cref="Exception">Thrown if <c>_registersReserve</c> is too small.</exception>
    private IReadOnlyDictionary<Register, Register> AssignReservedRegisters(IEnumerable<Register> registers)
    {
        return AssignReservedRegisters(registers, ImmutableDictionary<Register, Register>.Empty);
    }

    /// <summary>
    ///     Assigns to each register in <paramref name="registers"/>
    ///     an unique hardware register from <c>_registersReserve</c>.
    ///     If register has an assigned hardware register from reserve pool then it uses the same assigment.
    /// </summary>
    /// <exception cref="Exception">Thrown if <c>_registersReserve</c> is too small.</exception>
    private IReadOnlyDictionary<Register, Register> AssignReservedRegisters(IEnumerable<Register> registers, IReadOnlyDictionary<Register, Register> partial)
    {
        var registerSet = registers.ToHashSet();

        // narrow partial dictionary to only the keys from `registers`
        var reserveAssigment = partial
            .Where(entry => registerSet.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        var unassignedRegisters = registerSet.Where(register => !reserveAssigment.ContainsKey(register)).ToList();

        // skip registers in reserve which are already used in partial assigment
        var availableReserve = _registersReserve.Where(reserved => !reserveAssigment.ContainsValue(reserved)).ToList();

        if (unassignedRegisters.Count > availableReserve.Count)
        {
            throw new Exception(
                $"Not enough reserved registers, required {unassignedRegisters.Count}, but got {availableReserve.Count}");
        }

        // add missing assignments 
        foreach (var (register, reserve) in unassignedRegisters.Zip(availableReserve.ToList<Register>()))
        {
            reserveAssigment.Add(register, reserve);
        }

        return reserveAssigment;
    }
}
