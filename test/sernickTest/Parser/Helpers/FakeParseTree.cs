namespace sernickTest.Parser.Helpers;

using Input;
using sernick.Grammar.Syntax;
using sernick.Input;
using sernick.Parser.ParseTree;
using sernick.Utility;
using IParseTree = sernick.Parser.ParseTree.IParseTree<sernick.Grammar.Syntax.Symbol>;

public interface IFakeParseTree
{
    private protected static readonly Range<ILocation> Locations = new(new FakeLocation(), new FakeLocation());

    IParseTree Convert(IReadOnlyDictionary<Symbol, List<Production<Symbol>>> productions);
}

public sealed record FakeParseTreeNode(Symbol Symbol, int ProductionIndex, IEnumerable<IFakeParseTree> Children) : IFakeParseTree
{
    internal FakeParseTreeNode(Symbol symbol) : this(symbol, 0, Enumerable.Empty<FakeParseTreeNode>())
    {
    }

    internal FakeParseTreeNode(Symbol symbol, IEnumerable<IFakeParseTree> children) : this(symbol, 0, children)
    {
    }

    internal FakeParseTreeNode(Symbol symbol, IFakeParseTree child) : this(symbol, 0, child)
    {
    }

    internal FakeParseTreeNode(Symbol symbol, int productionIndex, IFakeParseTree child) : this(symbol, productionIndex, new[] { child })
    {
    }

    public IParseTree Convert(IReadOnlyDictionary<Symbol, List<Production<Symbol>>> productions) =>
        new ParseTreeNode<Symbol>(
            Symbol,
            productions[Symbol][ProductionIndex],
            Children.Select(child => child.Convert(productions)),
            IFakeParseTree.Locations);
}

public sealed record FakeParseTreeLeaf(Symbol Symbol) : IFakeParseTree
{
    public IParseTree Convert(IReadOnlyDictionary<Symbol, List<Production<Symbol>>> _) =>
        new ParseTreeLeaf<Symbol>(Symbol, IFakeParseTree.Locations);
}
