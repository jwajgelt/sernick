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

public class ParseTreeNodeTest
{
    private readonly Range<ILocation> _location = new(new FakeLocation(), new FakeLocation());

    [Fact]
    public void EqualEmptyNodes()
    {
        var node1 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>(),
            _location);
        var node2 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>(),
            _location);

        Assert.Equal(node1, node2);
    }

    [Fact]
    public void DifferentSymbolNodes()
    {
        var node1 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>(),
            _location);
        var node2 = new ParseTreeNode('B'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>(),
            _location);

        Assert.NotEqual(node1, node2);
    }

    [Fact]
    public void DifferentProductionNodes()
    {
        var node1 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>(),
            _location);
        var node2 = new ParseTreeNode('A'.ToCategory(),
            new Production('B'.ToCategory(), Regex.Empty),
            Array.Empty<IParseTree>(),
            _location);

        Assert.NotEqual(node1, node2);
    }

    [Fact]
    public void DifferentChildrenSizeNodes()
    {
        var leaf = new ParseTreeLeaf('X'.ToCategory(), _location);
        var node1 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            new[] { leaf, leaf },
            _location);
        var node2 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            new[] { leaf },
            _location);

        Assert.NotEqual(node1, node2);
    }

    [Fact]
    public void EqualNodesCase1()
    {
        var leaf1 = new ParseTreeLeaf('X'.ToCategory(), _location);
        var leaf2 = new ParseTreeLeaf('X'.ToCategory(), _location);
        var node1 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            new[] { leaf1 },
            _location);
        var node2 = new ParseTreeNode('A'.ToCategory(),
            new Production('A'.ToCategory(), Regex.Empty),
            new[] { leaf2 },
            _location);

        Assert.Equal(node1, node2);
    }

    /*
     *     A
     *     |
     *   B---C
     *   |
     *  DEF
     */
    [Fact]
    public void EqualNodesCase2()
    {
        var production = new Production('A'.ToCategory(), Regex.Empty);

        var leaf1 = new ParseTreeLeaf('D'.ToCategory(), _location);
        var leaf2 = new ParseTreeLeaf('E'.ToCategory(), _location);
        var leaf3 = new ParseTreeLeaf('F'.ToCategory(), _location);
        var node1 = new ParseTreeNode('B'.ToCategory(),
            production, new[] { leaf1, leaf2, leaf3 }, _location);
        var node2 = new ParseTreeNode('C'.ToCategory(),
            production, Array.Empty<IParseTree>(), _location);
        var root1 = new ParseTreeNode('A'.ToCategory(),
            production, new[] { node1, node2 }, _location);

        var leaf4 = new ParseTreeLeaf('D'.ToCategory(), _location);
        var leaf5 = new ParseTreeLeaf('E'.ToCategory(), _location);
        var leaf6 = new ParseTreeLeaf('F'.ToCategory(), _location);
        var node3 = new ParseTreeNode('B'.ToCategory(),
            production, new[] { leaf4, leaf5, leaf6 }, _location);
        var node4 = new ParseTreeNode('C'.ToCategory(),
            production, Array.Empty<IParseTree>(), _location);
        var root2 = new ParseTreeNode('A'.ToCategory(),
            production, new[] { node3, node4 }, _location);

        Assert.Equal(root1, root2);
    }
}
