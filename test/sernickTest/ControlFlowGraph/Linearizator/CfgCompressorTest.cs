namespace sernickTest.ControlFlowGraph.Linearizator;

using sernick.ControlFlowGraph.Analysis;
using sernick.ControlFlowGraph.CodeTree;

public class CfgCompressorTest
{
    [Fact]
    public void TestPath()
    {
        var p3 = new SingleExitNode(null, new List<CodeTreeValueNode> { 3 });
        var p2 = new SingleExitNode(p3, new List<CodeTreeValueNode> { 2 });
        var p1 = new SingleExitNode(p2, new List<CodeTreeValueNode> { 1 });

        var expected = new SingleExitNode(null, new List<CodeTreeValueNode> { 1, 2, 3 });

        var compressed = CfgCompressor.CompressPaths(p1);

        Assert.Equal(expected, compressed, new CfgIsomorphismComparer());
    }

    [Fact]
    public void TestConditional()
    {
        var p7 = new SingleExitNode(null, new List<CodeTreeValueNode> { 7 });
        var p6 = new SingleExitNode(null, new List<CodeTreeValueNode> { 6 });
        var p5 = new SingleExitNode(p7, new List<CodeTreeValueNode> { 5 });
        var p4 = new SingleExitNode(p6, new List<CodeTreeValueNode> { 4 });
        var p3 = new ConditionalJumpNode(p4, p5, 3);
        var p2 = new SingleExitNode(p3, new List<CodeTreeValueNode> { 2 });
        var p1 = new SingleExitNode(p2, new List<CodeTreeValueNode> { 1 });

        var expected = new SingleExitNode(
            new ConditionalJumpNode(
                new SingleExitNode(null, new List<CodeTreeValueNode> { 4, 6 }),
                new SingleExitNode(null, new List<CodeTreeValueNode> { 5, 7 }),
                3),
            new List<CodeTreeValueNode> { 1, 2 });

        var compressed = CfgCompressor.CompressPaths(p1);

        Assert.Equal(expected, compressed, new CfgIsomorphismComparer());
    }

    [Fact]
    public void TestLoop()
    {
        var p5 = new SingleExitNode(null, new List<CodeTreeValueNode> { 5 });
        var p4 = new SingleExitNode(p5, new List<CodeTreeValueNode> { 4 });
        var p3 = new SingleExitNode(p4, new List<CodeTreeValueNode> { 3 });
        var p2 = new SingleExitNode(p3, new List<CodeTreeValueNode> { 2 });
        var p1 = new SingleExitNode(p2, new List<CodeTreeValueNode> { 1 });

        p5.NextTree = p3;

        var e3 = new SingleExitNode(null, new List<CodeTreeValueNode> { 3, 4, 5 });
        e3.NextTree = e3;
        var expected = new SingleExitNode(e3, new List<CodeTreeValueNode> { 1, 2 });

        var compressed = CfgCompressor.CompressPaths(p1);

        Assert.Equal(expected, compressed, new CfgIsomorphismComparer());
    }

    [Fact]
    public void TestComplex()
    {
        var p7 = new SingleExitNode(null, new List<CodeTreeValueNode> { 7 });
        var p6 = new SingleExitNode(p7, new List<CodeTreeValueNode> { 6 });
        var p5 = new SingleExitNode(null, new List<CodeTreeValueNode> { 5 });
        var p4 = new ConditionalJumpNode(p5, p6, 4);
        var p3 = new SingleExitNode(p4, new List<CodeTreeValueNode> { 3 });
        var p2 = new SingleExitNode(p3, new List<CodeTreeValueNode> { 2 });
        var p1 = new SingleExitNode(p2, new List<CodeTreeValueNode> { 1 });

        p5.NextTree = p3;

        var e5 = new SingleExitNode(null, new List<CodeTreeValueNode> { 5 });
        var e3 = new SingleExitNode(
            new ConditionalJumpNode(e5, new SingleExitNode(null, new List<CodeTreeValueNode> { 6, 7 }), 4),
            new List<CodeTreeValueNode> { 3 });
        e5.NextTree = e3;
        var expected = new SingleExitNode(e3, new List<CodeTreeValueNode> { 1, 2 });

        var compressed = CfgCompressor.CompressPaths(p1);

        Assert.Equal(expected, compressed, new CfgIsomorphismComparer());
    }
}
