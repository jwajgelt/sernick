namespace sernickTest.Parser.Helpers;

using Input;
using sernick.Common.Regex;
using sernick.Grammar.Syntax;
using sernick.Input;
using sernick.Parser.ParseTree;
using sernick.Utility;
using IParseTree = sernick.Parser.ParseTree.IParseTree<sernick.Grammar.Syntax.Symbol>;

public interface IFakeParseTree
{
    public static readonly Range<ILocation> Locations = new(new FakeLocation(), new FakeLocation());

    IParseTree Convert(IReadOnlyDictionary<Symbol, List<Production<Symbol>>> productions);
    IParseTree Convert();
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
            Children.Select(child => child.Convert(productions)).ToList(),
            IFakeParseTree.Locations);

    public IParseTree Convert() =>
        new ParseTreeNode<Symbol>(
            Symbol,
            new FakeProduction(),
            Children.Select(child => child.Convert()).ToList(),
            IFakeParseTree.Locations);
}

public sealed record FakeParseTreeLeaf(Symbol Symbol) : IFakeParseTree
{
    public IParseTree Convert(IReadOnlyDictionary<Symbol, List<Production<Symbol>>> _) =>
        new ParseTreeLeaf<Symbol>(Symbol, IFakeParseTree.Locations);

    public IParseTree Convert() => new ParseTreeLeaf<Symbol>(Symbol, IFakeParseTree.Locations);
}

public sealed record FakeSymbol : Symbol;

public sealed record FakeProduction() : Production<Symbol>(new FakeSymbol(), Regex<Symbol>.Empty);
