namespace sernick.Compiler.Instruction;

using CodeGeneration;
using CodeGeneration.InstructionSelection;
using ControlFlowGraph.CodeTree;
using Function;
using static CodeGeneration.InstructionSelection.CodeTreePatternPredicates;
using Bin = BinaryOpInstruction;
using Mov = MovInstruction;
using Pat = CodeGeneration.InstructionSelection.CodeTreePattern;

public static class SernickInstructionSet
{
    public static IEnumerable<CodeTreePatternRule> Rules
    {
        get
        {
            // mov $reg, *
            {
                yield return new CodeTreePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg, Pat.WildcardNode),
                    (inputs, values) => new List<IInstruction>
                    {
                        Mov.ToReg(values.Get<Register>(reg)).FromReg(inputs[0])
                    });
            }

            // mov $reg, [*]
            {
                yield return new CodeTreePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg, Pat.MemoryRead(Pat.WildcardNode)),
                    (inputs, values) => new List<IInstruction>
                    {
                        Mov.ToReg(values.Get<Register>(reg)).FromMem(inputs[0])
                    });
            }

            // mov [*], *
            {
                yield return new CodeTreePatternRule(
                    Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, _) => new List<IInstruction>
                    {
                        Mov.ToMem(inputs[0]).FromReg(inputs[1])
                    });
            }

            // mov $reg, $const
            {
                yield return new CodeTreePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg,
                        Pat.Constant(Any<RegisterValue>(), out var imm)),
                    (_, values) => new List<IInstruction>
                    {
                        Mov.ToReg(values.Get<Register>(reg)).FromImm(values.Get<RegisterValue>(imm))
                    });
            }

            // mov [*], $const
            {
                yield return new CodeTreePatternRule(
                    Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out var imm)),
                    (inputs, values) => new List<IInstruction>
                    {
                        Mov.ToMem(inputs[0]).FromImm(values.Get<RegisterValue>(imm))
                    });
            }

            // add *, *
            {
                yield return new CodeTreePatternRule(
                    Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _, Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, _) => new List<IInstruction>
                    {
                        Bin.Add.ToReg(inputs[0]).FromReg(inputs[1])
                    });
            }

            // call $label
            {
                yield return new CodeTreePatternRule(
                    Pat.FunctionCall(out var call),
                    (_, values) => new List<IInstruction>
                    {
                        new CallInstruction(values.Get<IFunctionCaller>(call).Label)
                    });
            }
        }
    }
}
