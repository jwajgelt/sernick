namespace sernickTest.Parser.ParseTree;

using Helpers;
using Input;
using IParseTree = sernick.Parser.ParseTree.IParseTree<Helpers.CharCategory>;
using ParseTreeLeaf = sernick.Parser.ParseTree.ParseTreeLeaf<Helpers.CharCategory>;
using ParseTreeNode = sernick.Parser.ParseTree.ParseTreeNode<Helpers.CharCategory>;
using Production = sernick.Grammar.Syntax.Production<Helpers.CharCategory>;
using Regex = sernick.Common.Regex.Regex<Helpers.CharCategory>;

public class ParseTreeLeafTest
{
    [Fact]
    public void EqualNodes()
    {
        var location = new FakeLocation();
        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaf2 = new ParseTreeLeaf('A'.ToCategory(), location, location);

        Assert.Equal(leaf1, leaf2);
    }

    [Fact]
    public void UnequalNodes()
    {
        var location = new FakeLocation();
        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaf2 = new ParseTreeLeaf('B'.ToCategory(), location, location);

        Assert.NotEqual(leaf1, leaf2);
    }

    [Fact]
    public void DifferentTypes()
    {
        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var node = new ParseTreeNode('A'.ToCategory(), location, location,
            new Production('A'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>());

        leaf.Equals(node);
        Assert.NotEqual<IParseTree>(leaf, node);
    }
}
