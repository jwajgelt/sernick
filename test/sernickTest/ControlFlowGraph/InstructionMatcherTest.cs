namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static sernick.ControlFlowGraph.CodeTreePatternPredicates;
using Pat = sernick.ControlFlowGraph.CodeTreePattern;

public class InstructionMatcherTest
{
    [Theory]
    [MemberData(nameof(TestData))]
    public void TestMatchPattern(CodeTreePattern pattern, CodeTreeNode codeTree)
    {
        var instructionMatcher = new InstructionMatcher(new[] { pattern });
        Assert.True(instructionMatcher.MatchCodeTree(codeTree, out _));
    }

    public static readonly IEnumerable<object[]> TestData = new[]
    {
        // mov $reg, *
        new object[]
        {
            Pat.RegisterWrite(Any<Register>(), Pat.WildcardNode),
            Reg(new Register()).Write(5 + 5)
        },
        
        // mov $reg, [*]
        new object[]
        {
            Pat.RegisterWrite(Any<Register>(), Pat.MemoryRead(Pat.WildcardNode)),
            Reg(new Register()).Write(Mem(5).Value)
        },
        
        // mov [*], *
        new object[]
        {
            Pat.MemoryWrite(Pat.WildcardNode, Pat.WildcardNode),
            Mem(5).Write(5 + 5)
        },
        
        // mov $reg, $const
        new object[] {
            Pat.RegisterWrite(Any<Register>(), Pat.Constant(Any<RegisterValue>())),
            Reg(new Register()).Write(5)
        },
        
        // mov [*], $const
        new object[] {
            Pat.MemoryWrite(Pat.WildcardNode, Pat.Constant(Any<RegisterValue>())),
            Mem(5).Write(5)
        },

        // mov $reg, 0 === xor $reg, $reg
        new object[]
        {
            Pat.RegisterWrite(Any<Register>(), Pat.Constant(IsZero)),
            Reg(new Register()).Write(0)
        },
        
        // add *, *
        new object[] {
            Pat.BinaryOperationNode(Is(BinaryOperation.Add), Pat.WildcardNode, Pat.WildcardNode),
            Reg(new Register()).Read() + 5
        }
    };
}
