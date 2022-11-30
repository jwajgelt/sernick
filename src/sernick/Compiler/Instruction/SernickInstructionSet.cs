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

            // call $label
            {
                yield return new CodeTreePatternRule(
                    Pat.FunctionCall(out var call),
                    (_, values) => new List<IInstruction>
                    {
                        new CallInstruction(values.Get<IFunctionCaller>(call).Label)
                    });
            }

            // <op> *, *
            {
                yield return new CodeTreePatternRule(
                    Pat.BinaryOperationNode(
                        IsAnyOf(
                            BinaryOperation.Add, BinaryOperation.Sub,
                            BinaryOperation.BitwiseAnd, BinaryOperation.BitwiseOr), out var op,
                        Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, values) => new List<IInstruction>
                    {
                        values.Get<BinaryOperation>(op) switch
                        {
                            BinaryOperation.Add => Bin.Add.ToReg(inputs[0]).FromReg(inputs[1]),
                            BinaryOperation.Sub => Bin.Sub.ToReg(inputs[0]).FromReg(inputs[1]),
                            BinaryOperation.BitwiseAnd => Bin.And.ToReg(inputs[0]).FromReg(inputs[1]),
                            BinaryOperation.BitwiseOr => Bin.Or.ToReg(inputs[0]).FromReg(inputs[1]),
                            _ => throw new ArgumentOutOfRangeException()
                        }
                    });
            }

            /* CONDITIONALS */

            // cmp *, *
            // set<cc> $out
            {
                yield return new CodeTreePatternRule(
                    Pat.BinaryOperationNode(
                        IsAnyOf(
                            BinaryOperation.Equal, BinaryOperation.NotEqual,
                            BinaryOperation.LessThan, BinaryOperation.GreaterThan,
                            BinaryOperation.LessThanEqual, BinaryOperation.GreaterThanEqual), out var op,
                        Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, values) => new List<IInstruction>
                    {
                        Bin.Cmp.ToReg(inputs[0]).FromReg(inputs[1]),
                        values.Get<BinaryOperation>(op) switch
                        {
                            BinaryOperation.Equal => new SetCcInstruction(ConditionCode.E, inputs[0]),
                            BinaryOperation.NotEqual => new SetCcInstruction(ConditionCode.Ne, inputs[0]),
                            BinaryOperation.LessThan => new SetCcInstruction(ConditionCode.L, inputs[0]),
                            BinaryOperation.GreaterThan => new SetCcInstruction(ConditionCode.G, inputs[0]),
                            BinaryOperation.LessThanEqual => new SetCcInstruction(ConditionCode.Ng, inputs[0]),
                            BinaryOperation.GreaterThanEqual => new SetCcInstruction(ConditionCode.Nl, inputs[0]),
                            _ => throw new ArgumentOutOfRangeException()
                        }
                    });
            }
        }
    }
}
