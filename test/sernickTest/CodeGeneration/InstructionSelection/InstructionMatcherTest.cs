namespace sernickTest.CodeGeneration.InstructionSelection;

using Compiler.Function.Helpers;
using sernick.CodeGeneration;
using sernick.CodeGeneration.InstructionSelection;
using sernick.Compiler.Function;
using sernick.Compiler.Instruction;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternPredicates;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using Bin = sernick.Compiler.Instruction.BinaryOpInstruction;
using Mov = sernick.Compiler.Instruction.MovInstruction;
using Pat = sernick.CodeGeneration.InstructionSelection.CodeTreePattern;

public class CodeTreePatternMatcherTest
{
    [Theory]
    [MemberData(nameof(TestData))]
    public void TestMatchPattern(CodeTreePatternRule rule, CodeTreeNode codeTree,
        IEnumerable<CodeTreeValueNode> expectedLeaves)
    {
        var codeTreeMatcher = new CodeTreePatternMatcher(new[] { rule });
        Assert.True(codeTreeMatcher.MatchCodeTree(codeTree, out var leaves, out _));
        Assert.Equal(expectedLeaves, leaves);
    }

    private static readonly Register register = new();

    public static readonly IEnumerable<object[]> TestData = new[]
    {
        // mov $reg, *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out var reg1, Pat.WildcardNode),
                (inputs, values) => new List<IInstruction>
                {
                    Mov.ToReg(values.Get<Register>(reg1)).FromReg(inputs[0])
                }),
            Reg(register).Write((CodeTreeValueNode)5 + 5),
            new[] { (CodeTreeValueNode)5 + 5 }
        },
        
        // mov $reg, [*]
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out var reg2, Pat.MemoryRead(Pat.WildcardNode)),
                (inputs, values) => new List<IInstruction>
                {
                    Mov.ToReg(values.Get<Register>(reg2)).FromMem(inputs[0])
                }),
            Reg(register).Write(Mem(5).Value),
            new[] { (CodeTreeValueNode)5 }
        },
        
        // mov [*], *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
                (inputs, _) => new List<IInstruction>
                {
                    Mov.ToMem(inputs[0]).FromReg(inputs[1])
                }),
            Mem(5).Write((CodeTreeValueNode)5 + 5),
            new[] { (CodeTreeValueNode)5, (CodeTreeValueNode)5 + 5 }
        },
        
        // mov $reg, $const
        new object[] {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out var reg4, Pat.Constant(Any<RegisterValue>(), out var imm4)),
                (_, values) => new List<IInstruction>
                {
                    Mov.ToReg(values.Get<Register>(reg4)).FromImm(values.Get<RegisterValue>(imm4))
                }),
            Reg(register).Write(5),
            Enumerable.Empty<CodeTreeValueNode>()
        },
        
        // mov [*], $const
        new object[] {
            new CodeTreePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out var imm5)),
                (inputs, values) => new List<IInstruction>
                {
                    Mov.ToMem(inputs[0]).FromImm(values.Get<RegisterValue>(imm5))
                }),
            Mem(5).Write(5),
            new[] { (CodeTreeValueNode)5 }
        },

        // mov $reg, 0 === xor $reg, $reg
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out var reg6, Pat.Constant(IsZero, out _)),
                (_, values) => new List<IInstruction>
                {
                    Bin.Xor.ToReg(values.Get<Register>(reg6)).FromReg(values.Get<Register>(reg6))
                }),
            Reg(register).Write(0),
            Enumerable.Empty<CodeTreeValueNode>()
        },
        
        // add *, *
        new object[] {
            new CodeTreePatternRule(
                Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _, Pat.WildcardNode, Pat.WildcardNode),
                (inputs, _) => new List<IInstruction>
                {
                    Bin.Add.ToReg(inputs[0]).FromReg(inputs[1])
                }),
            Reg(register).Read() + 5,
            new[] { Reg(register).Read(), (CodeTreeValueNode)5 }
        },
        
        // call *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.FunctionCall(out var call8),
                (_, values) => new List<IInstruction>
                {
                    new CallInstruction(values.Get<IFunctionCaller>(call8).Label)
                }),
            new FunctionCall(new FakeFunctionContext()),
            Enumerable.Empty<CodeTreeValueNode>()
        }
    };
}

internal static class ValuesHelper
{
    public static TValue Get<TValue>(this IReadOnlyDictionary<CodeTreePattern, object> dictionary, CodeTreePattern key)
    {
        var value = dictionary[key];
        if (value is null)
        {
            throw new KeyNotFoundException();
        }

        return (TValue)value;
    }
}
