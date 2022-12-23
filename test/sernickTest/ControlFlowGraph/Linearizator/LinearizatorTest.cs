namespace sernickTest.ControlFlowGraph.Linearizator;

using Moq;
using sernick.CodeGeneration;
using sernick.ControlFlowGraph.Analysis;
using sernick.ControlFlowGraph.CodeTree;

public class LinearizatorTest
{
    private sealed record IdentityInstructionNode(CodeTreeRoot Node) : IInstruction
    {
        public IEnumerable<Register> RegistersDefined => Enumerable.Empty<Register>();
        public IEnumerable<Register> RegistersUsed => Enumerable.Empty<Register>();
        public bool PossibleFollow => true;
        public Label? PossibleJump => null;
        public bool IsCopy => false;

        public IInstruction MapRegisters(IReadOnlyDictionary<Register, Register> map) => new Mock<IInstruction>().Object;

        public string ToAsm(IReadOnlyDictionary<Register, HardwareRegister> registerMapping) => "";
    }

    private static Mock<IInstructionCovering> InstructionCoveringMock()
    {
        var mockedInstructionCovering = new Mock<IInstructionCovering>();
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<SingleExitNode>(), It.IsAny<Label>())).Returns
            ((SingleExitNode node, Label _) => new List<IdentityInstructionNode> { new(node) });
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<ConditionalJumpNode>(), It.IsAny<Label>(), It.IsAny<Label>())).Returns
            ((ConditionalJumpNode node, Label _, Label _) => new List<IdentityInstructionNode> { new(node) });
        return mockedInstructionCovering;
    }

    [Fact]
    public void TestPath()
    {
        var mockedInstructionCovering = InstructionCoveringMock();
        var linearizator = new Linearizator(mockedInstructionCovering.Object);

        var emptyOperationsList = new List<CodeTreeNode>();
        // path p1 -> p2 -> p3
        var p3 = new SingleExitNode(null, emptyOperationsList);
        var p2 = new SingleExitNode(p3, emptyOperationsList);
        var p1 = new SingleExitNode(p2, emptyOperationsList);

        var actual = linearizator.Linearize(p1).ToList();
        var numUniqueNodes = 3;
        var numExpectedLabels = 2;

        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count);

        Assert.Same(p1, (actual[0] as IdentityInstructionNode)?.Node);
        Assert.IsType<Label>(actual[1]);
        Assert.Same(p2, (actual[2] as IdentityInstructionNode)?.Node);
        Assert.IsType<Label>(actual[3]);
        Assert.Same(p3, (actual[4] as IdentityInstructionNode)?.Node);
    }

    [Fact]
    public void TestOneConditionalNode()
    {
        var mockedInstructionCovering = InstructionCoveringMock();
        var linearizator = new Linearizator(mockedInstructionCovering.Object);

        var emptyOperationsList = new List<CodeTreeNode>();
        // conditional node, in both cases we have "null" as the next node
        var trueCaseNode = new SingleExitNode(null, emptyOperationsList);
        var falseCaseNode = new SingleExitNode(null, emptyOperationsList);
        var mockedValueNode = new Mock<CodeTreeValueNode>();
        var conditionalNode = new ConditionalJumpNode(trueCaseNode, falseCaseNode, mockedValueNode.Object);

        var actual = linearizator.Linearize(conditionalNode).ToList();

        var numUniqueNodes = 3;
        var numExpectedLabels = 2;

        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count());
        Assert.Same(conditionalNode, (actual[0] as IdentityInstructionNode)?.Node);
        Assert.IsType<Label>(actual[1]);
        Assert.Same(trueCaseNode, (actual[2] as IdentityInstructionNode)?.Node);
        Assert.IsType<Label>(actual[3]);
        Assert.Same(falseCaseNode, (actual[4] as IdentityInstructionNode)?.Node);
    }

    [Fact]
    public void TestTwoConditionalNodesAndTwoSingleExitNodes()
    {
        var mockedInstructionCovering = InstructionCoveringMock();
        var linearizator = new Linearizator(mockedInstructionCovering.Object);

        var emptyOperationsList = new List<CodeTreeNode>();
        var mockedValueNode = new Mock<CodeTreeValueNode>();

        // conditionalNode1        
        //       /\           
        //      T  F
        //     /    \
        //    CN2    S1
        //
        //
        // conditionalNode2 (CN2)     
        //       /\
        //      T  F
        //     /    \
        //    S1    S2
        //
        //
        //   S1 and S2 below are independent in the graph
        //   SingleExitNode S1 -> finish
        //   SingleExitNode S2 -> finish
        var s1 = new SingleExitNode(null, emptyOperationsList);
        var s2 = new SingleExitNode(null, emptyOperationsList);
        var cn2 = new ConditionalJumpNode(s1, s2, mockedValueNode.Object);
        var cn1 = new ConditionalJumpNode(cn2, s1, mockedValueNode.Object);

        var actual = linearizator.Linearize(cn1).ToList();

        var numUniqueNodes = 4;
        var numExpectedLabels = 3;
        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count());

        // CN1 -- only "instruction set" without a label
        Assert.Same(cn1, (actual[0] as IdentityInstructionNode)?.Node);

        // next in DFS order is CN2, with a label
        Assert.IsType<Label>(actual[1]);
        Assert.Same(cn2, (actual[2] as IdentityInstructionNode)?.Node);

        // next in DFS order is S1 (as child of CN2), with a label
        Assert.IsType<Label>(actual[3]);
        Assert.Same(s1, (actual[4] as IdentityInstructionNode)?.Node);

        // next in DFS order is S2 (as child of CN2), with a label
        Assert.IsType<Label>(actual[5]);
        Assert.Same(s2, (actual[6] as IdentityInstructionNode)?.Node);
    }

    [Fact]
    public void TestMultipleConditionalNodes()
    {
        var mockedInstructionCovering = InstructionCoveringMock();
        var linearizator = new Linearizator(mockedInstructionCovering.Object);

        var emptyOperationsList = new List<CodeTreeNode>();
        var mockedValueNode = new Mock<CodeTreeValueNode>();

        //       CN1        
        //       /\           
        //      T  F
        //     /    \
        //    CN2    S1
        //
        //       CN2    
        //       /\
        //      T  F
        //     /    \
        //    CN3    S2
        //
        //       CN3    
        //       /\
        //      T  F
        //     /    \
        //    S1    S2
        //
        //   S1 and S2 below are independent in the graph
        //   SingleExitNode S1 -> finish
        //   SingleExitNode S2 -> finish
        var s1 = new SingleExitNode(null, emptyOperationsList);
        var s2 = new SingleExitNode(null, emptyOperationsList);
        var cn3 = new ConditionalJumpNode(s1, s2, mockedValueNode.Object);
        var cn2 = new ConditionalJumpNode(cn3, s2, mockedValueNode.Object);
        var cn1 = new ConditionalJumpNode(cn2, s1, mockedValueNode.Object);

        var actual = linearizator.Linearize(cn1).ToList();

        var numUniqueNodes = 5;
        var numExpectedLabels = 4;

        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count());

        // CN1 -- only "instruction set" without a label
        Assert.Same(cn1, (actual[0] as IdentityInstructionNode)?.Node);

        // next in DFS order is CN2, with a label (child of CN1)
        Assert.IsType<Label>(actual[1]);
        Assert.Same(cn2, (actual[2] as IdentityInstructionNode)?.Node);

        // next in DFS order is CN3, with a label (child of CN2)
        Assert.IsType<Label>(actual[3]);
        Assert.Same(cn3, (actual[4] as IdentityInstructionNode)?.Node);

        // next in DFS order is S1 (as child of CN3), with a label
        Assert.IsType<Label>(actual[5]);
        Assert.Same(s1, (actual[6] as IdentityInstructionNode)?.Node);

        // next in DFS order is S2 (as child of CN3), with a label
        Assert.IsType<Label>(actual[7]);
        Assert.Same(s2, (actual[8] as IdentityInstructionNode)?.Node);
    }
}

