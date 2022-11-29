namespace sernickTest.CodeGeneration.InstructionSelection;

using sernick.CodeGeneration;
using sernick.CodeGeneration.InstructionSelection;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternPredicates;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using Pat = sernick.CodeGeneration.InstructionSelection.CodeTreePattern;

public class CodeTreePatternMatcherTest
{
    [Theory]
    [MemberData(nameof(TestData))]
    public void TestMatchPattern(CodeTreePatternRule rule, CodeTreeNode codeTree)
    {
        var codeTreeMatcher = new CodeTreePatternMatcher(new[] { rule });
        Assert.True(codeTreeMatcher.MatchCodeTree(codeTree, out _, out _));
    }

    public static readonly IEnumerable<object[]> TestData = new[]
    {
        // mov $reg, *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.WildcardNode),
                _ => Enumerable.Empty<IInstruction>()),
            Reg(new Register()).Write(5 + 5)
        },
        
        // mov $reg, [*]
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.MemoryRead(Pat.WildcardNode)),
                    _ => Enumerable.Empty<IInstruction>()),
            Reg(new Register()).Write(Mem(5).Value)
        },
        
        // mov [*], *
        new object[]
        {
            new CodeTreePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
                _ => Enumerable.Empty<IInstruction>()),
            Mem(5).Write(5 + 5)
        },
        
        // mov $reg, $const
        new object[] {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(Any<RegisterValue>(), out _)),
                _ => Enumerable.Empty<IInstruction>()),
            Reg(new Register()).Write(5)
        },
        
        // mov [*], $const
        new object[] {
            new CodeTreePatternRule(
                Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>(), out _)),
                _ => Enumerable.Empty<IInstruction>()),
            Mem(5).Write(5)
        },

        // mov $reg, 0 === xor $reg, $reg
        new object[]
        {
            new CodeTreePatternRule(
                Pat.RegisterWrite(Any<Register>(), out _, Pat.Constant(IsZero, out _)),
                _ => Enumerable.Empty<IInstruction>()),
            Reg(new Register()).Write(0)
        },
        
        // add *, *
        new object[] {
            new CodeTreePatternRule(
                Pat.BinaryOperationNode(Is(BinaryOperation.Add), out _, Pat.WildcardNode, Pat.WildcardNode),
                _ => Enumerable.Empty<IInstruction>()),
            Reg(new Register()).Read() + 5
        }
    };
}
