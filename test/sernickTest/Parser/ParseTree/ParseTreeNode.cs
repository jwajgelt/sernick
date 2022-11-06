namespace sernickTest.Parser.ParseTree;

using Helpers;
using Input;

using Regex = sernick.Common.Regex.Regex<Helpers.CharCategory>;
using IParseTree = sernick.Parser.ParseTree.IParseTree<Helpers.CharCategory>;
using ParseTreeNode = sernick.Parser.ParseTree.ParseTreeNode<Helpers.CharCategory>;
using ParseTreeLeaf = sernick.Parser.ParseTree.ParseTreeLeaf<Helpers.CharCategory>;
using Production = sernick.Grammar.Syntax.Production<Helpers.CharCategory>;

public class ParseTreeNodeTest
{
    [Fact]
    public void EqualEmptyNodes()
    {
        var location = new FakeLocation();
        var node1 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            Array.Empty<IParseTree>());
        var node2 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            Array.Empty<IParseTree>());
        
        Assert.Equal(node1, node2);
    }
    
    [Fact]
    public void DifferentSymbolNodes()
    {
        var location = new FakeLocation();
        var node1 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            Array.Empty<IParseTree>());
        var node2 =  new ParseTreeNode('B'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            Array.Empty<IParseTree>());
        
        Assert.NotEqual(node1, node2);
    }
    
    [Fact]
    public void DifferentProductionNodes()
    {
        var location = new FakeLocation();
        var node1 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            Array.Empty<IParseTree>());
        var node2 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('B'.ToCategory(), Regex.Empty), 
            Array.Empty<IParseTree>());
        
        Assert.NotEqual(node1, node2);
    }
    
    [Fact]
    public void DifferentChildrenSizeNodes()
    {
        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('X'.ToCategory(), location, location);
        var node1 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            new[] {leaf, leaf});
        var node2 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            new[] {leaf});
        
        Assert.NotEqual(node1, node2);
    }
    
    [Fact]
    public void EqualNodesCase1()
    {
        var location = new FakeLocation();
        var leaf1 = new ParseTreeLeaf('X'.ToCategory(), location, location);
        var leaf2 = new ParseTreeLeaf('X'.ToCategory(), location, location);
        var node1 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            new[] {leaf1});
        var node2 =  new ParseTreeNode('A'.ToCategory(), location, location, 
            new Production('A'.ToCategory(), Regex.Empty), 
            new[] {leaf2});
        
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
        var location = new FakeLocation();
        var production = new Production('A'.ToCategory(), Regex.Empty);
        
        var leaf1 = new ParseTreeLeaf('D'.ToCategory(), location, location);
        var leaf2 = new ParseTreeLeaf('E'.ToCategory(), location, location);
        var leaf3 = new ParseTreeLeaf('F'.ToCategory(), location, location);
        var node1 = new ParseTreeNode('B'.ToCategory(), location, location, 
            production, new[] {leaf1, leaf2, leaf3});
        var node2 = new ParseTreeNode('C'.ToCategory(), location, location, 
            production, Array.Empty<IParseTree>());
        var root1 = new ParseTreeNode('A'.ToCategory(), location, location, 
            production, new[] {node1, node2});
        
        var leaf4 = new ParseTreeLeaf('D'.ToCategory(), location, location);
        var leaf5 = new ParseTreeLeaf('E'.ToCategory(), location, location);
        var leaf6 = new ParseTreeLeaf('F'.ToCategory(), location, location);
        var node3 = new ParseTreeNode('B'.ToCategory(), location, location, 
            production, new[] {leaf4, leaf5, leaf6});
        var node4 = new ParseTreeNode('C'.ToCategory(), location, location, 
            production, Array.Empty<IParseTree>());
        var root2 = new ParseTreeNode('A'.ToCategory(), location, location, 
            production, new[] {node3, node4});
        
        Assert.Equal(root1, root2);
    }
}
