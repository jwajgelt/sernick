namespace sernickTest.CodeGeneration.InstructionSelection;

using Compiler.Function.Helpers;
using Moq;
using sernick.CodeGeneration.InstructionSelection;
using sernick.ControlFlowGraph.CodeTree;
using sernick.Utility;
using Utility;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternPredicates;
using static sernick.Compiler.PlatformConstants;
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

    [Fact]
    public void TestMatchOpRegConst()
    {
        // <op> $reg, $const
        CodeTreePattern? reg = null;
        var rule = Pat.RegisterWrite(Any<Register>(), out reg,
            Pat.BinaryOperationNode(
                IsAnyOf(
                    BinaryOperation.Add, BinaryOperation.Sub,
                    BinaryOperation.BitwiseAnd, BinaryOperation.BitwiseOr), out _,
                Pat.RegisterRead(SameAsIn<Register>(() => reg!), out _),
                Pat.Constant(Any<RegisterValue>(), out _))).AsRule();

        var codeTree = Reg(register).Write(Reg(register).Read() - POINTER_SIZE);

        TestMatchPattern(rule, codeTree, Enumerable.Empty<CodeTreeValueNode>());
    }

    private static readonly Register register = new();
    private static readonly GlobalAddress address = new("addr");

    public static readonly (
        CodeTreePatternRule rule,
        CodeTreeNode tree,
        IEnumerable<CodeTreeValueNode> expectedLeaves
    )[] TestData = {
        // $const
        (
            Pat.Constant(Any<RegisterValue>(), out _).AsRule(),
            new Constant(new RegisterValue(1)),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // reg
        (
            Pat.RegisterRead(Any<Register>(), out _).AsRule(),
            Reg(register).Read(),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // [*]
        (
            Pat.MemoryRead(Pat.WildcardNode).AsRule(),
            Mem(Reg(register).Read()).Read(),
            Reg(register).Read().Enumerate()
        ),
        
        // mov $reg, *
        (
            Pat.RegisterWrite(Any<Register>(), out _, Pat.WildcardNode).AsRule(),
            Reg(register).Write((CodeTreeValueNode)5 + 5),
            new[] { (CodeTreeValueNode)5 + 5 }
        ),
        
        // mov $reg, [*]
        (
            Pat.RegisterWrite(Any<Register>(), out _, Pat.MemoryRead(Pat.WildcardNode)).AsRule(),
            Reg(register).Write(Mem(5).Value),
            new CodeTreeValueNode[] { 5 }
        ),
        
        // mov [*], *
        (
            Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode).AsRule(),
            Mem(5).Write((CodeTreeValueNode)5 + 5),
            new CodeTreeValueNode[] { 5, (CodeTreeValueNode)5 + 5 }
        ),
        
        // mov $reg, $const
        (
            Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(Any<RegisterValue>(), out _)).AsRule(),
            Reg(register).Write(5),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // mov $reg, [$addr + $displacement]
        (
            Pat.RegisterWrite(Any<Register>(), out _,
                Pat.MemoryRead(Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _,
                    Pat.GlobalAddress(out _),
                    CodeTreePattern.Constant(Any<RegisterValue>(), out _)))).AsRule(),
            Reg(register).Write(Mem(address + POINTER_SIZE).Read()),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // mov [$addr + $displacement], *
        (
            Pat.MemoryWrite(Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _,
                    Pat.GlobalAddress(out _), CodeTreePattern.Constant(Any<RegisterValue>(), out _)),
                Pat.WildcardNode).AsRule(),
            Mem(address + POINTER_SIZE).Write(Reg(register).Read()),
            Reg(register).Read().Enumerate()
        ),
        
        // mov [*], $const
        (
            Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out _)).AsRule(),
            Mem(5).Write(5),
            new CodeTreeValueNode[] { 5 }
        ),

        // mov $reg, 0 === xor $reg, $reg
        (
            Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(IsZero, out _)).AsRule(),
            Reg(register).Write(0),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // add *, *
        (
            Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _, Pat.WildcardNode, Pat.WildcardNode).AsRule(),
            Reg(register).Read() + 5,
            new CodeTreeValueNode[] { Reg(register).Read(), 5 }
        ),
        
        // neg *
        (
            Pat.UnaryOperationNode(Is(UnaryOperation.Negate), out _, Pat.WildcardNode).AsRule(),
            ~Reg(register).Read(),
            new CodeTreeValueNode[] { Reg(register).Read() }
        ),
        
        // call $label
        (
            Pat.FunctionCall(out _).AsRule(),
            new FunctionCall(new FakeFunctionContext()),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // ret
        (
            Pat.FunctionReturn.AsRule(),
            new FunctionReturn(),
            Enumerable.Empty<CodeTreeValueNode>()
        ),
        
        // cmp *, *
        // set<cc> *
        (
            Pat.BinaryOperationNode(
                    IsAnyOf(
                        BinaryOperation.Equal, BinaryOperation.NotEqual,
                        BinaryOperation.LessThan, BinaryOperation.GreaterThan,
                        BinaryOperation.LessThanEqual, BinaryOperation.GreaterThanEqual), out _,
                    Pat.WildcardNode, Pat.WildcardNode).AsRule(),
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
