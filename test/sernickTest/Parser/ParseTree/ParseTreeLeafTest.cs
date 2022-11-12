namespace sernickTest.Parser.ParseTree;

using Helpers;
using Input;
using sernick.Input;
using sernick.Utility;
using IParseTree = sernick.Parser.ParseTree.IParseTree<Helpers.CharCategory>;
using ParseTreeLeaf = sernick.Parser.ParseTree.ParseTreeLeaf<Helpers.CharCategory>;
using ParseTreeNode = sernick.Parser.ParseTree.ParseTreeNode<Helpers.CharCategory>;
using Production = sernick.Grammar.Syntax.Production<Helpers.CharCategory>;
using Regex = sernick.Common.Regex.Regex<Helpers.CharCategory>;

public class ParseTreeLeafTest
{
    private readonly Range<ILocation> _location = new(new FakeLocation(), new FakeLocation());
    
    [Fact]
    public void EqualNodes()
    {
        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaf2 = new ParseTreeLeaf('A'.ToCategory(), _location);

        Assert.Equal(leaf1, leaf2);
    }

    [Fact]
    public void UnequalNodes()
    {
        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaf2 = new ParseTreeLeaf('B'.ToCategory(), _location);

        Assert.NotEqual(leaf1, leaf2);
    }

    [Fact]
    public void DifferentTypes()
    {
        var leaf = new ParseTreeLeaf('A'.ToCategory(), _location);
        var node = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>(),
            _location);

        Assert.NotEqual<IParseTree>(leaf, node);
    }
}
