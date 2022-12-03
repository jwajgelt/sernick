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

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode>{raxRead});
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode>{rbxRead});

        var comparer = new CfgIsomorphismComparer();
        Assert.False(comparer.Equals(cfgA, cfgB));
    }
    
    [Fact]
    public void SingleRegisterCompare()
    {
        var aRegRead = Reg(new Register()).Read();
        var bRegRead = Reg(new Register()).Read();

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode>{aRegRead});
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode>{bRegRead});

        var comparer = new CfgIsomorphismComparer();
        Assert.True(comparer.Equals(cfgA, cfgB));
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

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode>{aRegRead, aRegWrite});
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode>{bRegRead, bRegWrite});

        var comparer = new CfgIsomorphismComparer();
        Assert.True(comparer.Equals(cfgA, cfgB));
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

        var cfgA = new SingleExitNode(null, new List<CodeTreeNode>{aRegRead, cRegWrite});
        var cfgB = new SingleExitNode(null, new List<CodeTreeNode>{bRegRead, bRegWrite});

        var comparer = new CfgIsomorphismComparer();
        Assert.False(comparer.Equals(cfgA, cfgB));
    }
}