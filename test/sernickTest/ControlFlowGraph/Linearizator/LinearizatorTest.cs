namespace sernickTest.ControlFlowGraph.Linearizator;

using System;
using Diagnostics;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.CallGraph;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.TypeChecking;
using sernick.Ast.Analysis.VariableAccess;
using sernick.Ast.Nodes;
using sernick.CodeGeneration;
using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;
using sernick.ControlFlowGraph.Analysis;
using static Ast.Helpers.AstNodesExtensions;
using static sernick.Compiler.PlatformConstants;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;


public class LinearizatorTest
{
    [Fact]
    public void TestPath()
    {
        var mockedInstructionCovering = new Mock<IInstructionCovering>();
        // it is not important what's inside the list of instruction
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<SingleExitNode>(), It.IsAny<Label>())).Returns(new List<IInstruction>());
        var linearizator = new Linearizator(mockedInstructionCovering.Object);

        var emptyOperationsList = new List<CodeTreeNode>();
        // path p1 -> p2 -> p3
        var p3 = new SingleExitNode(null, emptyOperationsList);
        var p2 = new SingleExitNode(p3, emptyOperationsList);
        var p1 = new SingleExitNode(p2, emptyOperationsList);


        var actual = linearizator.Linearize(p1);
        Assert.Equal(2, actual.Count()); // empty lists for inside node covers, so only two jumps should be
        Assert.All(actual, instruction => Assert.IsType<Label>(instruction));
    }

    [Fact]
    public void TestOneConditionalNode()
    {
        var mockedInstructionCovering = new Mock<IInstructionCovering>();
        // it is not important what's inside the list of instruction
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<SingleExitNode>(), It.IsAny<Label>())).Returns(new List<IInstruction>());
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<ConditionalJumpNode>(), It.IsAny<Label>(), It.IsAny<Label>())).Returns(new List<IInstruction>());
        var linearizator = new Linearizator(mockedInstructionCovering.Object);

        var emptyOperationsList = new List<CodeTreeNode>();
        // conditional node, in both cases we have "null" as the next node
        var trueCaseNode = new SingleExitNode(null, emptyOperationsList);
        var falseCaseNode = new SingleExitNode(null, emptyOperationsList);
        var mockedValueNode = new Mock<CodeTreeValueNode>();
        var conditionalNode = new ConditionalJumpNode(trueCaseNode,falseCaseNode, mockedValueNode.Object);


        var actual = linearizator.Linearize(conditionalNode);
        Assert.Equal(2, actual.Count()); // we only expect labels for true and false case to be added
        Assert.All(actual, instruction => Assert.IsType<Label>(instruction));
    }

    [Fact]
    public void TestTwoConditionalNodesAndTwoSingleExitNodes()
    {
        var mockedInstructionCovering = new Mock<IInstructionCovering>();
        // it is not important what's inside the list of instruction
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<SingleExitNode>(), It.IsAny<Label>())).Returns(new List<IInstruction>());
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<ConditionalJumpNode>(), It.IsAny<Label>(), It.IsAny<Label>())).Returns(new List<IInstruction>());
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
        Assert.Equal(3, actual.Count()); // 3 labels, not 4, since S1 is already visited as false case of CN1
        Assert.All(actual, instruction => Assert.IsType<Label>(instruction));
    }

    [Fact]
    public void TestMultipleConditionalNodes()
    {
        var mockedInstructionCovering = new Mock<IInstructionCovering>();
        // it is not important what's inside the list of instruction
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<SingleExitNode>(), It.IsAny<Label>())).Returns(new List<IInstruction>());
        mockedInstructionCovering.Setup(ic => ic.Cover(It.IsAny<ConditionalJumpNode>(), It.IsAny<Label>(), It.IsAny<Label>())).Returns(new List<IInstruction>());
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
        Assert.Equal(4, actual.Count()); // 4 labels, not 6
        Assert.All(actual, instruction => Assert.IsType<Label>(instruction));
    }
}

