namespace sernickTest.Ast.Analysis.ControlFlowGraph;

using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.ControlFlowGraph;
using sernick.Ast.Nodes;
using sernick.ControlFlowGraph.CodeTree;
using static Helpers.AstNodesExtensions;

public class FunctionCodeTreeGeneratorTest
{
    [Fact]
    public void GeneratesAllCallGraphs()
    {
        var tree = Program(
            Fun<IntType>("f").Body(
                Fun<BoolType>("g").Body(Return(Literal(true))).Get(out var funG),
                Return(42)
            ).Get(out var funF)
        );
        var graphF = new Mock<CodeTreeRoot>().Object;
        var graphG = new Mock<CodeTreeRoot>().Object;

        CodeTreeRoot Unravel(FunctionDefinition definition)
        {
            if (definition == funF)
            {
                return graphF;
            }

            if (definition == funG)
            {
                return graphG;
            }

            return new Mock<CodeTreeRoot>().Object;
        }

        var result = FunctionCodeTreeMapGenerator.Process(tree, Unravel);

        Assert.Equal(3, result.Count);  // main, f, g
        Assert.Same(graphF, result[funF]);
        Assert.Same(graphG, result[funG]);
    }
}
