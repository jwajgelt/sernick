namespace sernick.Compiler.Instruction;

using CodeGeneration;
using ControlFlowGraph.CodeTree;

public static class InstuctionHelper
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
}
