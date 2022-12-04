namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph.CodeTree;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;

public class CfgIsomorphismComparerTest
{
    [Fact]
    public void HardwareRegisterDiff()
    {
        var raxRead = Reg(HardwareRegister.RAX).Read();
        var rbxRead = Reg(HardwareRegister.RBX).Read();

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode> { raxRead });
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode> { rbxRead });

        Assert.NotEqual(cfgA, cfgB, new CfgIsomorphismComparer());
    }

    [Fact]
    public void SingleRegisterCompare()
    {
        var aRegRead = Reg(new Register()).Read();
        var bRegRead = Reg(new Register()).Read();

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode> { aRegRead });
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode> { bRegRead });

        Assert.Equal(cfgA, cfgB, new CfgIsomorphismComparer());
    }

    [Fact]
    public void TwoMatchingRegisterCompare()
    {
        var aReg = Reg(new Register());
        var bReg = Reg(new Register());

        var aRegRead = aReg.Read();
        var bRegRead = bReg.Read();

        var aRegWrite = aReg.Write(5);
        var bRegWrite = bReg.Write(5);

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode> { aRegRead, aRegWrite });
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode> { bRegRead, bRegWrite });

        Assert.Equal(cfgA, cfgB, new CfgIsomorphismComparer());
    }

    [Fact]
    public void TwoNonMatchingRegisterCompare()
    {
        var aReg = Reg(new Register());
        var cReg = Reg(new Register());

        var bReg = Reg(new Register());

        var aRegRead = aReg.Read();
        var bRegRead = bReg.Read();

        var cRegWrite = cReg.Write(5);
        var bRegWrite = bReg.Write(5);

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode> { aRegRead, cRegWrite });
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode> { bRegRead, bRegWrite });

        Assert.NotEqual(cfgA, cfgB, new CfgIsomorphismComparer());
    }

    [Fact]
    public void EquivalentCycles()
    {
        var aReg = Reg(new Register());
        var cReg = Reg(new Register());

        var bReg = Reg(new Register());
        var dReg = Reg(new Register());

        var aNodeOne = new SingleExitNode(null, new List<CodeTreeNode> { aReg.Read() });
        var aNodeTwo = new SingleExitNode(aNodeOne, new List<CodeTreeNode> { cReg.Read() });
        var aNodeThree = new SingleExitNode(aNodeTwo, new List<CodeTreeNode> { aReg.Read() });
        aNodeOne.NextTree = aNodeThree;

        var bNodeOne = new SingleExitNode(null, new List<CodeTreeNode> { bReg.Read() });
        var bNodeTwo = new SingleExitNode(bNodeOne, new List<CodeTreeNode> { dReg.Read() });
        var bNodeThree = new SingleExitNode(bNodeTwo, new List<CodeTreeNode> { bReg.Read() });
        bNodeOne.NextTree = bNodeThree;

        // Assert.Equal causes stack overflow due to default ToString implementation
        var comparer = new CfgIsomorphismComparer();
        Assert.True(comparer.Equals(aNodeThree, bNodeThree));
    }

    [Fact]
    public void DifferentCycles()
    {
        var aReg = Reg(new Register());
        var cReg = Reg(new Register());

        var bReg = Reg(new Register());
        var dReg = Reg(new Register());

        var aNodeOne = new SingleExitNode(null, new List<CodeTreeNode> { aReg.Read() });
        var aNodeTwo = new SingleExitNode(aNodeOne, new List<CodeTreeNode> { cReg.Read() });
        var aNodeThree = new SingleExitNode(aNodeTwo, new List<CodeTreeNode> { aReg.Read() });
        aNodeOne.NextTree = aNodeThree;

        var bNodeOne = new SingleExitNode(null, new List<CodeTreeNode> { bReg.Read() });
        var bNodeTwo = new SingleExitNode(bNodeOne, new List<CodeTreeNode> { dReg.Read() });
        var bNodeThree = new SingleExitNode(bNodeTwo, new List<CodeTreeNode> { bReg.Read() });
        bNodeOne.NextTree = bNodeTwo;

        var comparer = new CfgIsomorphismComparer();
        Assert.False(comparer.Equals(aNodeThree, bNodeThree));
    }
}
