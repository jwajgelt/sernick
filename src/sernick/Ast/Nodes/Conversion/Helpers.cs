namespace sernick.Ast.Nodes.Conversion;

using Grammar.Lexicon;
using Grammar.Syntax;
using Parser.ParseTree;

public static class Helpers
{
    /// <summary>
    /// Converts IParseTree to an Identifier.
    /// Requires that node has a valid Terminal symbol for an Identifier.
    /// </summary>
    public static Identifier ToIdentifier(this IParseTree<Symbol> node) => node.Symbol switch
    {
        Terminal { Category: LexicalGrammarCategory.VariableIdentifiers, Text: var text }
            => new Identifier(text, node.LocationRange),
        _ => throw new AggregateException("Invalid ParseTree for Identifier")
    };

    /// <summary>
    /// Skips first and last elements of nodes.
    /// Used to extract content from node list of the form:
    /// [bracket, ..., bracket]
    /// </summary>
    public static IEnumerable<IParseTree<Symbol>> SkipBraces(this IEnumerable<IParseTree<Symbol>> nodes)
        => nodes.Skip(1).SkipLast(1);

    /// <summary>
    /// Skips elements on odd indexes.
    /// Used to extract content from list of the form:
    /// [exp, ",", exp, ",", ...]
    /// </summary>
    public static IEnumerable<IParseTree<Symbol>> SkipCommas(this IEnumerable<IParseTree<Symbol>> nodes)
        => nodes.Where((_, index) => index % 2 == 0);

    public static IEnumerable<Expression> SelectExpressions(this IEnumerable<IParseTree<Symbol>> trees)
        => trees.Select(ExpressionConversion.ToExpression);

    public static Expression Join(this IEnumerable<Expression> expressions)
        => expressions.Aggregate((left, right) => new ExpressionJoin(left, right));

    public static IReadOnlyList<IParseTree<Symbol>> ChildrenList(this IParseTree<Symbol> node)
        => node.Children.ToList();
}
