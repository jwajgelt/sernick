namespace sernickTest.CodeGeneration.InstructionSelection;

using Compiler.Function.Helpers;
using Moq;
using sernick.CodeGeneration.InstructionSelection;
using sernick.ControlFlowGraph.CodeTree;
using sernick.Utility;
using Utility;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternPredicates;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using Pat = sernick.CodeGeneration.InstructionSelection.CodeTreePattern;

public class CodeTreePatternMatcherTest
{
    [Theory]
    [MemberTupleData(nameof(TestData))]
    public void TestMatchPattern(CodeTreePatternRule rule, CodeTreeNode codeTree, IEnumerable<CodeTreeValueNode> expectedLeaves)
    {
        Assert.True(rule.TryMatchCodeTreeNode(codeTree, out var leaves, out _));
        Assert.Equal(expectedLeaves, leaves);
    }

    private static readonly Register register = new();

    public static readonly (
        CodeTreePatternRule rule,
        CodeTreeNode tree,
        IEnumerable<CodeTreeValueNode> expectedLeaves
    )[] TestData = {
        // mov $reg, *
        (
            new CodeTreeNodePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.WildcardNode),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write((CodeTreeValueNode)5 + 5),
            new[] { (CodeTreeValueNode)5 + 5 }
        ),
        
        // mov $reg, [*]
        (
            new CodeTreeNodePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.MemoryRead(Pat.WildcardNode)),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write(Mem(5).Value),
            new CodeTreeValueNode[] { 5 }
        ),
        
        // mov [*], *
        (
            new CodeTreeNodePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Mem(5).Write((CodeTreeValueNode)5 + 5),
            new CodeTreeValueNode[] { 5, (CodeTreeValueNode)5 + 5 }
        ),
        
        // mov $reg, $const
        (
            new CodeTreeNodePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(Any<RegisterValue>(), out _)),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write(5),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // mov [*], $const
        (
            new CodeTreeNodePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out _)),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Mem(5).Write(5),
            new CodeTreeValueNode[] { 5 }
        ),

        // mov $reg, 0 === xor $reg, $reg
        (
            new CodeTreeNodePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(IsZero, out _)),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Write(0),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // add *, *
        (
            new CodeTreeNodePatternRule(
                Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _, Pat.WildcardNode, Pat.WildcardNode),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Read() + 5,
            new CodeTreeValueNode[] { Reg(register).Read(), 5 }
        ),
        
        // call $label
        (
            new CodeTreeNodePatternRule(
                Pat.FunctionCall(out _),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            new FunctionCall(new FakeFunctionContext()),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // cmp *, *
        // set<cc> *
        (
            new CodeTreeNodePatternRule(
                Pat.BinaryOperationNode(
                    IsAnyOf(
                        BinaryOperation.Equal, BinaryOperation.NotEqual,
                        BinaryOperation.LessThan, BinaryOperation.GreaterThan,
                        BinaryOperation.LessThanEqual, BinaryOperation.GreaterThanEqual), out _,
                    Pat.WildcardNode, Pat.WildcardNode),
                new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object),
            Reg(register).Read() < 5,
            new CodeTreeValueNode[] { Reg(register).Read(), 5 }
        )
    };

    [Fact]
    public void TestMatchSingleExitPattern()
    {
        var rule = new SingleExitNodePatternRule(new Mock<SingleExitNodePatternRule.GenerateInstructionsDelegate>().Object);

        var codeTree = new SingleExitNode(
            nextTree: new SingleExitNode(null, new List<CodeTreeNode>()),
            operations: new List<CodeTreeNode> { Reg(register).Read() });

        Assert.True(rule.TryMatchSingleExitNode(codeTree, out var leaves, out _));
        Assert.Equal(Reg(register).Read().Enumerate(), leaves);
    }

    [Fact]
    public void TestMatchConditionalJumpPattern()
    {
        var rule = new ConditionalJumpNodePatternRule(new Mock<ConditionalJumpNodePatternRule.GenerateInstructionsDelegate>().Object);

        var codeTree = new ConditionalJumpNode(
            trueCase: new SingleExitNode(null, new List<CodeTreeNode>()),
            falseCase: new SingleExitNode(null, new List<CodeTreeNode>()),
            conditionEvaluation: Reg(register).Read());

        Assert.True(rule.TryMatchConditionalJumpNode(codeTree, out var leaves, out _));
        Assert.Equal(Reg(register).Read().Enumerate(), leaves);
    }
}
