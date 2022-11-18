namespace sernickTest.Parser.Helpers;

using IParseTree = sernick.Parser.ParseTree.IParseTree<sernick.Grammar.Syntax.Symbol>;
using ParseTreeLeaf = sernick.Parser.ParseTree.ParseTreeLeaf<sernick.Grammar.Syntax.Symbol>;
using ParseTreeNode = sernick.Parser.ParseTree.ParseTreeNode<sernick.Grammar.Syntax.Symbol>;

public sealed class ParseTreeStructuralComparer : IEqualityComparer<IParseTree>
{
    public bool Equals(IParseTree? x, IParseTree? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x switch
        {
            ParseTreeLeaf xLeaf when y is ParseTreeLeaf yLeaf => xLeaf.Equals(yLeaf),
            ParseTreeNode xNode when y is ParseTreeNode yNode =>
                x.Symbol.Equals(y.Symbol) && xNode.Children.SequenceEqual(yNode.Children, this),
            _ => false
        };
    }

    public int GetHashCode(IParseTree obj)
    {
        throw new NotImplementedException();
    }
}
