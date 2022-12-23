namespace sernick.Compiler.Instruction;

using CodeGeneration;
using CodeGeneration.InstructionSelection;
using ControlFlowGraph.CodeTree;
using Function;
using Utility;
using static CodeGeneration.InstructionSelection.CodeTreePatternPredicates;
using Bin = BinaryOpInstruction;
using Mov = MovInstruction;
using Pat = CodeGeneration.InstructionSelection.CodeTreePattern;
using Un = UnaryOpInstruction;

public static class SernickInstructionSet
{
    public static IEnumerable<CodeTreePatternRule> Rules
    {
        get
        {
            // $const
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.Constant(Any<RegisterValue>(), out var imm),
                    (_, values) =>
                    {
                        var output = new Register();
                        return new List<IInstruction>
                        {
                            Mov.ToReg(output).FromImm(values.Get<RegisterValue>(imm))
                        }.WithOutput(output);
                    });
            }

            // $reg
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.RegisterRead(Any<Register>(), out var reg),
                    (_, values) =>
                        Enumerable.Empty<IInstruction>()
                            .WithOutput(values.Get<Register>(reg)));
            }

            // [*]
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.MemoryRead(Pat.WildcardNode),
                    (inputs, _) =>
                    {
                        var output = new Register();
                        return new List<IInstruction>
                        {
                            Mov.ToReg(output).FromMem(inputs[0])
                        }.WithOutput(output);
                    });
            }

            // mov $reg, $const
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg,
                        Pat.Constant(Any<RegisterValue>(), out var imm)),
                    (_, values) =>
                        new List<IInstruction>
                        {
                            Mov.ToReg(values.Get<Register>(reg)).FromImm(values.Get<RegisterValue>(imm))
                        }.WithOutput(null));
            }

            // mov $reg, [$addr + $displacement]
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg,
                        Pat.MemoryRead(Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _,
                            Pat.GlobalAddress(out var addr), CodeTreePattern.Constant(Any<RegisterValue>(), out var imm)))),
                    (_, values) =>
                        new List<IInstruction>
                        {
                            Mov.ToReg(values.Get<Register>(reg))
                                .FromMem(values.Get<Label>(addr), (isNegative: false, values.Get<RegisterValue>(imm)))
                        }.WithOutput(null));
            }

            // mov $reg, [$reg + $displacement]
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg,
                        Pat.MemoryRead(Pat.BinaryOperationNode(IsAnyOf(BinaryOperation.Add, BinaryOperation.Sub),
                            out var op,
                            Pat.RegisterRead(Any<Register>(), out var reg2),
                            Pat.Constant(Any<RegisterValue>(), out var imm)))),
                    (_, values) =>
                        Mov
                            .ToReg(values.Get<Register>(reg))
                            .FromMem(values.Get<Register>(reg2),
                                (isNegative: values.Get<BinaryOperation>(op) == BinaryOperation.Sub, values.Get<RegisterValue>(imm)))
                            .Enumerate()
                            .WithOutput(null));
            }

            // mov $reg, [*]
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg, Pat.MemoryRead(Pat.WildcardNode)),
                    (inputs, values) =>
                        new List<IInstruction>
                        {
                            Mov.ToReg(values.Get<Register>(reg)).FromMem(inputs[0])
                        }.WithOutput(null));
            }

            // mov $reg, *
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.RegisterWrite(Any<Register>(), out var reg, Pat.WildcardNode),
                    (inputs, values) =>
                        new List<IInstruction>
                        {
                            Mov.ToReg(values.Get<Register>(reg)).FromReg(inputs[0])
                        }.WithOutput(null));
            }

            // mov [$addr + $displacement], *
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.MemoryWrite(Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _,
                        Pat.GlobalAddress(out var addr), CodeTreePattern.Constant(Any<RegisterValue>(), out var imm)),
                        Pat.WildcardNode),
                    (inputs, values) =>
                        new List<IInstruction>
                        {
                            Mov
                                .ToMem(values.Get<Label>(addr), (isNegative: false, values.Get<RegisterValue>(imm)))
                                .FromReg(inputs[0])
                        }.WithOutput(null));
            }

            // mov [$reg + $displacement], $reg
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.MemoryWrite(Pat.BinaryOperationNode(IsAnyOf(BinaryOperation.Add, BinaryOperation.Sub),
                            out var op,
                            Pat.RegisterRead(Any<Register>(), out var reg),
                            Pat.Constant(Any<RegisterValue>(), out var imm)),
                        Pat.RegisterRead(Any<Register>(), out var reg2)),
                    (_, values) =>
                        Mov
                            .ToMem(values.Get<Register>(reg),
                                (isNegative: values.Get<BinaryOperation>(op) == BinaryOperation.Sub, values.Get<RegisterValue>(imm)))
                            .FromReg(values.Get<Register>(reg2))
                            .Enumerate()
                            .WithOutput(null));
            }

            // mov [*], $const
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out var imm)),
                    (inputs, values) =>
                        new List<IInstruction>
                        {
                            Mov.ToMem(inputs[0]).FromImm(values.Get<RegisterValue>(imm))
                        }.WithOutput(null));
            }

            // mov [*], *
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, _) =>
                        new List<IInstruction>
                        {
                            Mov.ToMem(inputs[0]).FromReg(inputs[1])
                        }.WithOutput(null));
            }

            // call $label
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.FunctionCall(out var call),
                    (_, values) =>
                        new List<IInstruction>
                        {
                            new CallInstruction(values.Get<IFunctionCaller>(call).Label)
                        }.WithOutput(null));
            }

            // ret
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.FunctionReturn, (_, _) => new List<IInstruction>
                    {
                        new RetInstruction()
                    }.WithOutput(null));
            }

            // <op> *, *
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.BinaryOperationNode(
                        IsAnyOf(
                            BinaryOperation.Add, BinaryOperation.Sub,
                            BinaryOperation.BitwiseAnd, BinaryOperation.BitwiseOr), out var op,
                        Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, values) =>
                        new List<IInstruction>
                        {
                            values.Get<BinaryOperation>(op) switch
                            {
                                BinaryOperation.Add => Bin.Add.ToReg(inputs[0]).FromReg(inputs[1]),
                                BinaryOperation.Sub => Bin.Sub.ToReg(inputs[0]).FromReg(inputs[1]),
                                BinaryOperation.BitwiseAnd => Bin.And.ToReg(inputs[0]).FromReg(inputs[1]),
                                BinaryOperation.BitwiseOr => Bin.Or.ToReg(inputs[0]).FromReg(inputs[1]),
                                _ => throw new ArgumentOutOfRangeException()
                            }
                        }.WithOutput(inputs[0]));
            }

            // <op> *
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.UnaryOperationNode(Any<UnaryOperation>(), out var op,
                        Pat.WildcardNode),
                    (inputs, values) => new List<IInstruction>
                    {
                        values.Get<UnaryOperation>(op) switch
                        {
                            UnaryOperation.Not => Un.Not.Reg(inputs[0]),
                            UnaryOperation.Negate => Un.Neg.Reg(inputs[0]),
                            _ => throw new ArgumentOutOfRangeException()
                        }
                    }.WithOutput(inputs[0]));
            }

            /* CONDITIONALS */

            // cmp *, *
            // set<cc> $out
            {
                yield return new CodeTreeNodePatternRule(
                    Pat.BinaryOperationNode(
                        IsAnyOf(
                            BinaryOperation.Equal, BinaryOperation.NotEqual,
                            BinaryOperation.LessThan, BinaryOperation.GreaterThan,
                            BinaryOperation.LessThanEqual, BinaryOperation.GreaterThanEqual), out var op,
                        Pat.WildcardNode, Pat.WildcardNode),
                    (inputs, values) =>
                    {
                        var output = new Register();
                        return new List<IInstruction>
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
                        }.WithOutput(output);
                    });
            }

            /* ROOT */

            // jmp $label
            {
                yield return new SingleExitNodePatternRule(next => new List<IInstruction>
                    {
                        new JmpInstruction(next)
                    }
                );
            }

            // cmp *, 0
            // jg $trueLabel
            // jng $falseLabel
            {
                yield return new ConditionalJumpNodePatternRule((input, trueCase, falseCase) =>
                    new List<IInstruction>
                    {
                        Bin.Cmp.ToReg(input).FromImm(new RegisterValue(0)),
                        new JmpCcInstruction(ConditionCode.G, trueCase),
                        new JmpCcInstruction(ConditionCode.Ng, falseCase)
                    });
            }
        }
    }
}

public static class GenerateInstructionsHelper
{
    public static (IEnumerable<IInstruction> instructions, Register? output) WithOutput(
        this IEnumerable<IInstruction> instructions, Register? output) => (instructions, output);
}
