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
                    (inputs, values) => (
                        instructions: new List<IInstruction>
                        {
                            Mov.ToReg(values.Get<Register>(reg)).FromReg(inputs[0])
                        },
                        output: null));
            }

            // mov $reg, [*]
            {
                yield return new CodeTreePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg, Pat.MemoryRead(Pat.WildcardNode)),
                    (inputs, values) => (
                        instructions: new List<IInstruction>
                        {
                            Mov.ToReg(values.Get<Register>(reg)).FromMem(inputs[0])
                        },
                        output: null));
            }

            // mov [*], *
            {
                yield return new CodeTreePatternRule(
                    Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, _) => (
                        instructions: new List<IInstruction>
                        {
                            Mov.ToMem(inputs[0]).FromReg(inputs[1])
                        },
                        output: null));
            }

            // mov $reg, $const
            {
                yield return new CodeTreePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg,
                        Pat.Constant(Any<RegisterValue>(), out var imm)),
                    (_, values) => (
                        instructions: new List<IInstruction>
                        {
                            Mov.ToReg(values.Get<Register>(reg)).FromImm(values.Get<RegisterValue>(imm))
                        },
                        output: null));
            }

            // mov [*], $const
            {
                yield return new CodeTreePatternRule(
                    Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out var imm)),
                    (inputs, values) => (
                        instructions: new List<IInstruction>
                        {
                            Mov.ToMem(inputs[0]).FromImm(values.Get<RegisterValue>(imm))
                        },
                        output: null));
            }

            // call $label
            {
                yield return new CodeTreePatternRule(
                    Pat.FunctionCall(out var call),
                    (_, values) => (
                        instructions: new List<IInstruction>
                        {
                            new CallInstruction(values.Get<IFunctionCaller>(call).Label)
                        },
                        output: null));
            }

            // <op> *, *
            {
                yield return new CodeTreePatternRule(
                    Pat.BinaryOperationNode(
                        IsAnyOf(
                            BinaryOperation.Add, BinaryOperation.Sub,
                            BinaryOperation.BitwiseAnd, BinaryOperation.BitwiseOr), out var op,
                        Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, values) => (
                        instructions: new List<IInstruction>
                        {
                            values.Get<BinaryOperation>(op) switch
                            {
                                BinaryOperation.Add => Bin.Add.ToReg(inputs[0]).FromReg(inputs[1]),
                                BinaryOperation.Sub => Bin.Sub.ToReg(inputs[0]).FromReg(inputs[1]),
                                BinaryOperation.BitwiseAnd => Bin.And.ToReg(inputs[0]).FromReg(inputs[1]),
                                BinaryOperation.BitwiseOr => Bin.Or.ToReg(inputs[0]).FromReg(inputs[1]),
                                _ => throw new ArgumentOutOfRangeException()
                            }
                        },
                        output: inputs[0]));
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
                    (inputs, values) =>
                    {
                        var output = new Register();
                        return (
                            instructions: new List<IInstruction>
                            {
                                Bin.Cmp.ToReg(inputs[0]).FromReg(inputs[1]), // cmp in0, in1
                                Bin.Xor.ToReg(output).FromReg(output), // mov out, 0
                                values.Get<BinaryOperation>(op) switch // setcc out
                                {
                                    BinaryOperation.Equal => new SetCcInstruction(ConditionCode.E, output),
                                    BinaryOperation.NotEqual => new SetCcInstruction(ConditionCode.Ne, output),
                                    BinaryOperation.LessThan => new SetCcInstruction(ConditionCode.L, output),
                                    BinaryOperation.GreaterThan => new SetCcInstruction(ConditionCode.G, output),
                                    BinaryOperation.LessThanEqual => new SetCcInstruction(ConditionCode.Ng, output),
                                    BinaryOperation.GreaterThanEqual => new SetCcInstruction(ConditionCode.Nl, output),
                                    _ => throw new ArgumentOutOfRangeException()
                                }
                            },
                            output);
                    });
            }
        }
    }
}