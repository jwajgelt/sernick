namespace sernickTest.CodeGeneration.InstructionSelection;

using Compiler.Function.Helpers;
using Moq;
using sernick.CodeGeneration.InstructionSelection;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternPredicates;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using Pat = sernick.CodeGeneration.InstructionSelection.CodeTreePattern;

public class CodeTreePatternMatcherTest
{
    [Theory]
    [MemberData(nameof(TestData))]
    public void TestMatchPattern(CodeTreePatternRule rule, CodeTreeNode codeTree,
        IEnumerable<CodeTreeValueNode> expectedLeaves)
    {
        Assert.True(rule.TryMatchCodeTree(codeTree, out var leaves, out _));
        Assert.Equal(expectedLeaves, leaves);
    }

    private static readonly Register register = new();

    public static readonly IEnumerable<object[]> TestData = new[]
    {
        // mov $reg, *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.WildcardNode),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write((CodeTreeValueNode)5 + 5),
            new[] { (CodeTreeValueNode)5 + 5 }
        },
        
        // mov $reg, [*]
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.MemoryRead(Pat.WildcardNode)),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write(Mem(5).Value),
            new CodeTreeValueNode[] { 5 }
        },
        
        // mov [*], *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Mem(5).Write((CodeTreeValueNode)5 + 5),
            new CodeTreeValueNode[] { 5, (CodeTreeValueNode)5 + 5 }
        },
        
        // mov $reg, $const
        new object[] {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(Any<RegisterValue>(), out _)),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write(5),
            Enumerable.Empty<CodeTreeValueNode>()
        },
        
        // mov [*], $const
        new object[] {
            new CodeTreePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out _)),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Mem(5).Write(5),
            new CodeTreeValueNode[] { 5 }
        },

        // mov $reg, 0 === xor $reg, $reg
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(IsZero, out _)),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write(0),
            Enumerable.Empty<CodeTreeValueNode>()
        },
        
        // add *, *
        new object[] {
            new CodeTreePatternRule(
                Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _, Pat.WildcardNode, Pat.WildcardNode),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Read() + 5,
            new CodeTreeValueNode[] { Reg(register).Read(), 5 }
        },
        
        // call $label
        new object[]
        {
            new CodeTreePatternRule(
                Pat.FunctionCall(out _),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            new FunctionCall(new FakeFunctionContext()),
            Enumerable.Empty<CodeTreeValueNode>()
        },
        
        // cmp *, *
        // set<cc> *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.BinaryOperationNode(
                    IsAnyOf(
                        BinaryOperation.Equal, BinaryOperation.NotEqual,
                        BinaryOperation.LessThan, BinaryOperation.GreaterThan,
                        BinaryOperation.LessThanEqual, BinaryOperation.GreaterThanEqual), out _,
                    Pat.WildcardNode, Pat.WildcardNode),
                new Mock<CodeTreePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Read() < 5,
            new CodeTreeValueNode[] { Reg(register).Read(), 5 }
        }
    };
}
