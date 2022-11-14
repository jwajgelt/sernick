namespace sernick.Ast.Nodes.Conversion;

using Grammar.Syntax;
using Parser.ParseTree;

public static class KeywordsConversion
{
    public static Expression ToKeyword(this IParseTree<Symbol> node) => node switch
    {

    };
}
