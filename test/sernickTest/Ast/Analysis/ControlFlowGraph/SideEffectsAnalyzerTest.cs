namespace sernickTest.Ast.Analysis.ControlFlowGraph;

using Compiler.Function.Helpers;
using Helpers;
using sernick.Ast;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.ControlFlowGraph.CodeTree;
using static Helpers.AstNodesExtensions;
using FunctionCall = sernick.ControlFlowGraph.CodeTree.FunctionCall;

public class SideEffectsAnalyzerTest
{
    [Theory]
    [InlineData(Infix.Op.Plus, BinaryOperation.Add)]
    [InlineData(Infix.Op.Minus, BinaryOperation.Sub)]
    [InlineData(Infix.Op.Equals, BinaryOperation.Equal)]
    [InlineData(Infix.Op.Less, BinaryOperation.LessThan)]
    [InlineData(Infix.Op.Greater, BinaryOperation.GreaterThan)]
    [InlineData(Infix.Op.LessOrEquals, BinaryOperation.LessThanEqual)]
    [InlineData(Infix.Op.GreaterOrEquals, BinaryOperation.GreaterThanEqual)]
    public void BinaryOperationsAreCompiledIntoBinaryOperationNodes(Infix.Op astOp, BinaryOperation binOp)
    {
        var functionContext = new FakeFunctionContext();
        Fun<UnitType>("f")
            .Body(
                new Infix(Literal(1), Literal(2),astOp)
            ).Get(out var tree);
        var expected = new List<SingleExitNode>
        {
            new SingleExitNode(null, new []
            {
                new BinaryOperationNode(
                    binOp,
                    new Constant(new RegisterValue(1)),
                    new Constant(new RegisterValue(2)))
            })
        };

        var result = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, new NameResolutionResult(), functionContext);
        
        Assert.Equal(expected, result, new CodeTreeNodeComparer());
    }
    
    [Theory]
    [InlineData(Infix.Op.Plus, BinaryOperation.Add)]
    [InlineData(Infix.Op.Minus, BinaryOperation.Sub)]
    [InlineData(Infix.Op.Equals, BinaryOperation.Equal)]
    [InlineData(Infix.Op.Less, BinaryOperation.LessThan)]
    [InlineData(Infix.Op.Greater, BinaryOperation.GreaterThan)]
    [InlineData(Infix.Op.LessOrEquals, BinaryOperation.LessThanEqual)]
    [InlineData(Infix.Op.GreaterOrEquals, BinaryOperation.GreaterThanEqual)]
    public void BinaryOperationsAreCompiledIntoBinaryOperationNodesWithVariableReads(Infix.Op astOp, BinaryOperation binOp)
    {
        var functionContext = new FakeFunctionContext();
        Fun<UnitType>("f")
            .Body(
                Var("x", 1, out var declX),
                Var("y", 2, out var declY),
                new Infix(Value("x", out var xUse), Value("y", out var yUse), astOp)
            ).Get(out var tree);
        var expected = new List<SingleExitNode>
        {
            new (null, new []
            {
                new FakeVariableWrite(declX, new Constant(new RegisterValue(1))),
                new FakeVariableWrite(declY, new Constant(new RegisterValue(2)))
            }),
            new (null, new []
            {
                new BinaryOperationNode(
                    binOp,
                    new FakeVariableRead(declX),
                    new FakeVariableRead(declY))
            })
        };

        var nameResolution = new NameResolutionResult().WithVars(
            (xUse, declX),
            (yUse, declY));

        var result = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, nameResolution, functionContext);
        
        Assert.Equal(expected, result, new CodeTreeNodeComparer());
    }
}

public class CodeTreeNodeComparer : IEqualityComparer<CodeTreeNode>
{
    public bool Equals(CodeTreeNode? x, CodeTreeNode? y)
    {
        return x switch
        {
            null => false,
            ConditionalJumpNode conditionalJumpNode => throw new NotImplementedException(),
            SingleExitNode xNode =>
                y is SingleExitNode yNode
                && xNode.Operations.SequenceEqual(yNode.Operations, this),
            BinaryOperationNode xNode =>
                y is BinaryOperationNode yNode
                && yNode.Operation == xNode.Operation
                && Equals(xNode.Left, yNode.Left)
                && Equals(xNode.Right, yNode.Right),
            Constant xNode => 
                y is Constant yNode
                && yNode.Value.Equals(xNode.Value),
            MemoryRead xNode => throw new NotImplementedException(),
            RegisterRead xNode => throw new NotImplementedException(),
            UnaryOperationNode xNode => 
                y is UnaryOperationNode yNode
                && yNode.Operation == xNode.Operation
                && Equals(xNode.Operand, yNode.Operand),
            FakeVariableRead xNode => 
                y is FakeVariableRead yNode
                && xNode.Variable.Equals(yNode.Variable),
            CodeTreeValueNode xNode => throw new NotImplementedException(),
            FunctionCall xNode => throw new NotImplementedException(),
            MemoryWrite xNode => throw new NotImplementedException(),
            RegisterWrite xNode => throw new NotImplementedException(),
            FakeVariableWrite xNode =>
                y is FakeVariableWrite yNode
                && xNode.Variable.Equals(yNode.Variable),
            _ => throw new ArgumentOutOfRangeException(nameof(x))
        };
    }

    public int GetHashCode(CodeTreeNode obj)
    {
        throw new NotImplementedException();
    }
}
