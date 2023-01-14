namespace sernick.Compiler.Instruction;

using System.Diagnostics.CodeAnalysis;
using CodeGeneration;
using ControlFlowGraph.CodeTree;
using Function;

public static class InstructionHelper
{
    /// <summary>
    /// Used to filter out noop instructions from the generated ASM
    /// </summary>
    public static bool IsNoop(this IAsmable asmable, IReadOnlyDictionary<Register, HardwareRegister> regAllocation)
    {
        switch (asmable)
        {
            case MovInstruction mov:
                if (mov.Target is RegInstructionOperand lhsOperand && mov.Source is RegInstructionOperand rhsOperand)
                {
                    return regAllocation[lhsOperand.Register].Equals(regAllocation[rhsOperand.Register]);
                }

                return false;
            default:
                return false;
        }
    }

    public static CodeTreeNode? HandleSpillSpecialCases(this IInstruction instruction, IReadOnlyDictionary<Register, VariableLocation> spillsLocation)
    {
        bool IsSpilled(IInstructionOperand instructionOperand, [NotNullWhen(true)] out Register? register)
        {
            register = null;
            if (instructionOperand is not RegInstructionOperand regInstructionOperand || !spillsLocation.ContainsKey(regInstructionOperand.Register))
            {
                return false;
            }

            register = regInstructionOperand.Register;
            return true;
        }

        bool UsesMemory(IInstructionOperand instructionOperand) => instructionOperand is MemInstructionOperand || IsSpilled(instructionOperand, out _);

        switch (instruction)
        {
            case MovInstruction movInstruction:
                // if none or both of the use memory, then let the default implementation handle it
                if (UsesMemory(movInstruction.Source) == UsesMemory(movInstruction.Target))
                {
                    return null;
                }

                CodeTreeNode? tree = null;

                if (IsSpilled(movInstruction.Source, out var sourceRegister))
                {
                    if (movInstruction.Target is not RegInstructionOperand regInstructionOperand)
                    {
                        throw new Exception("The target of an operation with memory operand in the source must be a register");
                    }

                    tree = new RegisterWrite(regInstructionOperand.Register, spillsLocation[sourceRegister].GenerateRead());
                }

                if (IsSpilled(movInstruction.Target, out var register))
                {
                    tree = movInstruction.Source switch
                    {
                        ImmInstructionOperand immInstructionOperand => spillsLocation[register]
                            .GenerateWrite(immInstructionOperand.Value),
                        RegInstructionOperand regInstructionOperand => spillsLocation[register]
                            .GenerateWrite(new RegisterRead(regInstructionOperand.Register)),
                        _ => throw new Exception()
                    };
                }

                return tree;
            default:
                return null;
        }
    }
}
