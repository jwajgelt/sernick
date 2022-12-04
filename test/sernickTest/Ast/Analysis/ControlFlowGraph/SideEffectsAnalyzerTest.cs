namespace sernickTest.Ast.Analysis.ControlFlowGraph;

using Compiler.Function.Helpers;
using Helpers;
using sernick.Ast;
using sernick.Ast.Analysis.CallGraph;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.VariableAccess;
using sernick.Ast.Nodes;
using sernick.ControlFlowGraph.CodeTree;
using static Helpers.AstNodesExtensions;

public class SideEffectsAnalyzerTest
{
    private static readonly CallGraph callGraph = new CallGraph();
    private static readonly VariableAccessMap variableAccessMap = new VariableAccessMap();

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
        // 1 `astOp` 2
        var functionContext = new FakeFunctionContext();
        var functionContextMap = new FunctionContextMap();
        Fun<UnitType>("f")
            .Body(
                new Infix(Literal(1), Literal(2), astOp)
            ).Get(out var tree);
        var expected = new List<SingleExitNode>
        {
            new (null, new []
            {
                new BinaryOperationNode(
                    binOp,
                    new Constant(new RegisterValue(1)),
                    new Constant(new RegisterValue(2)))
            })
        };

        var result = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, new NameResolutionResult(), functionContext, functionContextMap, callGraph, variableAccessMap);

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
        var functionContextMap = new FunctionContextMap();
        // var x = 1; var y = 2;
        // x `astOp` y
        // The declarations can happen in the same tree,
        // since they don't read each other.
        // The binary operation cannot happen in the same tree
        // as the variable declarations.
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

        var result = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, nameResolution, functionContext, functionContextMap, callGraph, variableAccessMap);

        Assert.Equal(expected, result, new CodeTreeNodeComparer());
    }

    [Theory(Skip = "Waiting for isomorphisms")]
    [InlineData(Infix.Op.Plus, BinaryOperation.Add)]
    [InlineData(Infix.Op.Minus, BinaryOperation.Sub)]
    [InlineData(Infix.Op.Equals, BinaryOperation.Equal)]
    [InlineData(Infix.Op.Less, BinaryOperation.LessThan)]
    [InlineData(Infix.Op.Greater, BinaryOperation.GreaterThan)]
    [InlineData(Infix.Op.LessOrEquals, BinaryOperation.LessThanEqual)]
    [InlineData(Infix.Op.GreaterOrEquals, BinaryOperation.GreaterThanEqual)]
#pragma warning disable xUnit1026
#pragma warning disable IDE0060
    public void BinaryOperationsShouldInsertTempVariablesForIntermediateResults(Infix.Op astOp, BinaryOperation binOp)
#pragma warning restore IDE0060
#pragma warning restore xUnit1026
    {
        var functionContext = new FakeFunctionContext();
        var functionContextMap = new FunctionContextMap();
        // var x = 1;
        // x `astOp` {x = x+1; x}
        // The left-hand-side of operation
        // "sees" the write in the right-hand-side.
        // It should write it to a temporary variable.
        Fun<UnitType>("f")
            .Body(
                Var("x", 1, out var declX),
                new Infix(
                    Value("x", out var xFirstUse),
                    Block(
                        "x".Assign(
                            Value("x", out var xSecondUse).Plus(1),
                            out var xAss
                        ),
                        Value("x", out var xThirdUse)
                        ),
                    astOp)
            ).Get(out var tree);

        var nameResolution = new NameResolutionResult().WithVars(
            (xFirstUse, declX),
            (xSecondUse, declX),
            (xThirdUse, declX))
            .WithAssigns((xAss, declX));
        _ = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, nameResolution, functionContext, functionContextMap, callGraph, variableAccessMap);
    }

    [Fact]
    public void AssignmentsAndReadsToSameVariableAreSeparated()
    {
        var functionContext = new FakeFunctionContext();
        var functionContextMap = new FunctionContextMap();
        // var x = 1;
        // var y = x
        // The `y` declaration cannot happen in the same tree as `x` declaration
        Fun<UnitType>("f")
            .Body(
                Var("x", 1, out var declX),
                Var<IntType>("y", Value("x", out var xUse), out var declY)
            ).Get(out var tree);
        var expected = new List<SingleExitNode>
        {
            new (null, new []
            {
                new FakeVariableWrite(declX, new Constant(new RegisterValue(1)))
            }),
            new (null, new []
            {
                new FakeVariableWrite(declY, new FakeVariableRead(declX))
            })
        };

        var nameResolution = new NameResolutionResult().WithVars((xUse, declX));

        var result = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, nameResolution, functionContext, functionContextMap, callGraph, variableAccessMap);

        Assert.Equal(expected, result, new CodeTreeNodeComparer());
    }

    [Fact]
    public void ReadsAndAssignmentsToSameVariableAreSeparated()
    {
        var functionContext = new FakeFunctionContext();
        var functionContextMap = new FunctionContextMap();
        // var x = 1;
        // var y = x;
        // x = y
        // The `x` write cannot happen in the same tree as `y` declaration
        Fun<UnitType>("f")
            .Body(
                Var("x", 1, out var declX),
                Var<IntType>("y", Value("x", out var xUse), out var declY),
                "x".Assign(Value("y", out var yUse), out var xAss)
            ).Get(out var tree);
        var expected = new List<SingleExitNode>
        {
            new (null, new []
            {
                new FakeVariableWrite(declX, new Constant(new RegisterValue(1)))
            }),
            new (null, new []
            {
                new FakeVariableWrite(declY, new FakeVariableRead(declX))
            }),
            new (null, new []
            {
                new FakeVariableWrite(declX, new FakeVariableRead(declY))
            })
        };

        var nameResolution = new NameResolutionResult()
            .WithVars((xUse, declX), (yUse, declY))
            .WithAssigns((xAss, declX));

        var result = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, nameResolution, functionContext, functionContextMap, callGraph, variableAccessMap);

        Assert.Equal(expected, result, new CodeTreeNodeComparer());
    }

    [Fact]
    public void AssignmentsCanUseSameVariableInRightHandSide()
    {
        var functionContext = new FakeFunctionContext();
        var functionContextMap = new FunctionContextMap();
        // var x = 1;
        // x = x+1
        // The assignment can happen in the same tree
        // as the right-hand side evaluation, since code trees
        // are evaluated bottom-up.
        Fun<UnitType>("f")
            .Body(
                Var("x", 1, out var declX),
                "x".Assign(Value("x", out var xUse).Plus(Literal(1)), out var xAss)
            ).Get(out var tree);
        var expected = new List<SingleExitNode>
        {
            new (null, new []
            {
                new FakeVariableWrite(declX, new Constant(new RegisterValue(1)))
            }),
            new (null, new []
            {
                new FakeVariableWrite(
                    declX,
                    new BinaryOperationNode(
                        BinaryOperation.Add,
                        new FakeVariableRead(declX),
                        new Constant(new RegisterValue(1)))
                    )
            })
        };

        var nameResolution = new NameResolutionResult()
            .WithVars((xUse, declX))
            .WithAssigns((xAss, declX));

        var result = SideEffectsAnalyzer.PullOutSideEffects(tree.Body, nameResolution, functionContext, functionContextMap, callGraph, variableAccessMap);

        Assert.Equal(expected, result, new CodeTreeNodeComparer());
    }
}
