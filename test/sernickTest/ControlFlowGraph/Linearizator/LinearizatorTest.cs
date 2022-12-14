namespace sernickTest.ControlFlowGraph.Linearizator;
using Moq;
using sernick.CodeGeneration;
using sernick.ControlFlowGraph.Analysis;
using sernick.ControlFlowGraph.CodeTree;

public class LinearizatorTest
{

    private class IdentityInstructionNode: IInstruction
    {
        public IEnumerable<Register> RegistersDefined { get; }
        public IEnumerable<Register> RegistersUsed { get; }
        public bool PossibleFollow { get; }
        public Label? PossibleJump { get; }
        public bool IsCopy { get; }
        public CodeTreeRoot Node { get; }
        public IdentityInstructionNode(CodeTreeRoot node)
        {
            Node = node;
            RegistersDefined = new List<Register>();
            RegistersUsed = new List<Register>();
        }
    }

    private Mock<IInstructionCovering> InstructionCoveringMock()
    {
        var mockedInstructionCovering = new Mock<IInstructionCovering>();
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<SingleExitNode>(), It.IsAny<Label>())).Returns
            ((SingleExitNode node, Label label) => {
                return new List<IdentityInstructionNode>() { new IdentityInstructionNode(node) };
            });
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<ConditionalJumpNode>(), It.IsAny<Label>(), It.IsAny<Label>())).Returns
            ((ConditionalJumpNode node, Label _, Label _) => {
                return new List<IdentityInstructionNode>() { new IdentityInstructionNode(node) };
            });
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

        var actual = linearizator.Linearize(p1);
        var numUniqueNodes = 3;
        var numExpectedLabels = 2;

        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count());

        Assert.Same(p1, (actual.ElementAt(0) as IdentityInstructionNode).Node);
        Assert.IsType<Label>(actual.ElementAt(1));
        Assert.Same(p2, (actual.ElementAt(2) as IdentityInstructionNode).Node);
        Assert.IsType<Label>(actual.ElementAt(3));
        Assert.Same(p3, (actual.ElementAt(4) as IdentityInstructionNode).Node);
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

        var actual = linearizator.Linearize(conditionalNode);

        var numUniqueNodes = 3;
        var numExpectedLabels = 2;

        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count());
        Assert.Same(conditionalNode, (actual.ElementAt(0) as IdentityInstructionNode).Node);
        Assert.IsType<Label>(actual.ElementAt(1));
        Assert.Same(trueCaseNode, (actual.ElementAt(2) as IdentityInstructionNode).Node);
        Assert.IsType<Label>(actual.ElementAt(3));
        Assert.Same(falseCaseNode, (actual.ElementAt(4) as IdentityInstructionNode).Node);
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
        var S1 = new SingleExitNode(null, emptyOperationsList);
        var S2 = new SingleExitNode(null, emptyOperationsList);
        var CN2 = new ConditionalJumpNode(S1, S2, mockedValueNode.Object);
        var CN1 = new ConditionalJumpNode(CN2, S1, mockedValueNode.Object);

        var actual = linearizator.Linearize(CN1);

        var numUniqueNodes = 4;
        var numExpectedLabels = 3;
        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count());

        // CN1 -- only "instruction set" without a label
        Assert.Same(CN1, (actual.ElementAt(0) as IdentityInstructionNode).Node);

        // next in DFS order is CN2, with a label
        Assert.IsType<Label>(actual.ElementAt(1));
        Assert.Same(CN2, (actual.ElementAt(2) as IdentityInstructionNode).Node);

        // next in DFS order is S1 (as child of CN2), with a label
        Assert.IsType<Label>(actual.ElementAt(3));
        Assert.Same(S1, (actual.ElementAt(4) as IdentityInstructionNode).Node);

        // next in DFS order is S2 (as child of CN2), with a label
        Assert.IsType<Label>(actual.ElementAt(5));
        Assert.Same(S2, (actual.ElementAt(6) as IdentityInstructionNode).Node);
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
        var S1 = new SingleExitNode(null, emptyOperationsList);
        var S2 = new SingleExitNode(null, emptyOperationsList);
        var CN3 = new ConditionalJumpNode(S1, S2, mockedValueNode.Object);
        var CN2 = new ConditionalJumpNode(CN3, S2, mockedValueNode.Object);
        var CN1 = new ConditionalJumpNode(CN2, S1, mockedValueNode.Object);

        var actual = linearizator.Linearize(CN1);

        var numUniqueNodes = 5;
        var numExpectedLabels = 4;

        Assert.Equal(numUniqueNodes + numExpectedLabels, actual.Count());

        // CN1 -- only "instruction set" without a label
        Assert.Same(CN1, (actual.ElementAt(0) as IdentityInstructionNode).Node);

        // next in DFS order is CN2, with a label (child of CN1)
        Assert.IsType<Label>(actual.ElementAt(1));
        Assert.Same(CN2, (actual.ElementAt(2) as IdentityInstructionNode).Node);

        // next in DFS order is CN3, with a label (child of CN2)
        Assert.IsType<Label>(actual.ElementAt(3));
        Assert.Same(CN3, (actual.ElementAt(4) as IdentityInstructionNode).Node);

        // next in DFS order is S1 (as child of CN3), with a label
        Assert.IsType<Label>(actual.ElementAt(5));
        Assert.Same(S1, (actual.ElementAt(6) as IdentityInstructionNode).Node);

        // next in DFS order is S2 (as child of CN3), with a label
        Assert.IsType<Label>(actual.ElementAt(7));
        Assert.Same(S2, (actual.ElementAt(8) as IdentityInstructionNode).Node);
    }
}

