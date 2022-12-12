namespace sernickTest.ControlFlowGraph;

using sernick.CodeGeneration;
using sernick.Compiler.Function;
using sernick.Compiler.Instruction;
using sernick.ControlFlowGraph.Analysis;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static sernickTest.Ast.Helpers.AstNodesExtensions;

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

        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        covering.Cover(node, new Label(""));
    }

    [Fact]
    public void CoversOperators()
    {
        var raxRead = Reg(HardwareRegister.RAX).Read();
        var rbxRead = Reg(HardwareRegister.RBX).Read();
        var regA = Reg(new Register());
        var memRead = Mem(regA.Read()).Read();

        var neg = new UnaryOperationNode(UnaryOperation.Not, raxRead);
        var addition = rbxRead + memRead;

        var node = new SingleExitNode(null, new List<CodeTreeNode> { neg, addition });

        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        covering.Cover(node, new Label(""));
    }

    [Fact]
    public void CoversConditionalJump()
    {
        var raxRead = Reg(HardwareRegister.RAX).Read();
        var regRead = Reg(new Register()).Read();

        var condition = raxRead < regRead;

        var exitNode = new SingleExitNode(null, new List<CodeTreeNode> { });
        var node = new ConditionalJumpNode(exitNode, exitNode, condition);

        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        covering.Cover(node, "true", "false");
    }

    [Fact]
    public void CoversFunctionCall()
    {
        var funFactory = new FunctionFactory((_, _) => "");
        var mainContext = funFactory.CreateFunction(null, Ident(""), new IFunctionParam[] { }, false);
        var call = new FunctionCall(mainContext);

        var node = new SingleExitNode(null, new List<CodeTreeNode> { call });

        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        covering.Cover(node, new Label(""));
    }

    [Fact]
    public void CoversGenerateCall()
    {
        var funFactory = new FunctionFactory((_, _) => "");
        var mainContext = funFactory.CreateFunction(null, Ident(""), new IFunctionParam[] { }, false);
        var callWithContext = mainContext.GenerateCall(new List<CodeTreeValueNode>());

        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        foreach (var node in callWithContext.CodeGraph)
        {
            covering.Cover(node, new Label(""));
        }
    }

    [Fact]
    public void CoversPrologueAndEpilogue()
    {
        var funFactory = new FunctionFactory((_, _) => "");
        var mainContext = funFactory.CreateFunction(null, Ident(""), new IFunctionParam[] { }, false);
        var prologue = mainContext.GeneratePrologue();
        var epilogue = mainContext.GenerateEpilogue(null);

        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        foreach (var node in prologue)
        {
            covering.Cover(node, new Label(""));
        }

        foreach (var node in epilogue)
        {
            covering.Cover(node, new Label(""));
        }
    }
}
