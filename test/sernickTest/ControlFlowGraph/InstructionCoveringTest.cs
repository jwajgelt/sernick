namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph.CodeTree;
using sernick.ControlFlowGraph.Analysis;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using sernick.Compiler.Instruction;
using sernick.CodeGeneration;

public class InstructionCoveringTest
{
    [Fact]
    public void CoversReadWrite()
    {
        var raxRead = Reg(HardwareRegister.RAX).Read();
        var rbxWrite = Reg(HardwareRegister.RBX).Write(1);
        var regA = Reg(new Register());
        var memWrite = Mem(regA.Read()).Write(2);

        var node = new SingleExitNode(null, new List<CodeTreeNode> { raxRead, rbxWrite, memWrite });

        InstructionCovering covering = new InstructionCovering(SernickInstructionSet.Rules);
        var instructions = covering.Cover(node, new Label(""));
    }

    [Fact(Skip = "debug?")]
    public void CoversOperators()
    {
        var raxRead = Reg(HardwareRegister.RAX).Read();
        var rbxRead = Reg(HardwareRegister.RBX).Read();
        var regA = Reg(new Register());
        var memRead = Mem(regA.Read()).Read();

        //var neg = new UnaryOperationNode(UnaryOperation.Not, raxRead);
        var addition = rbxRead + memRead;

        var node = new SingleExitNode(null, new List<CodeTreeNode> { /*neg,*/ addition });

        InstructionCovering covering = new InstructionCovering(SernickInstructionSet.Rules);
        var instructions = covering.Cover(node, new Label(""));
    }

    /*[Fact]
    public void CoversConditionalJump()
    {
        
    }

    [Fact]
    public void CoversFunctionCall()
    {
        
    }*/
}
