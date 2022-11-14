namespace sernick.Ast.Nodes.Conversion;

using System.Diagnostics;
using Grammar.Lexicon;
using Grammar.Syntax;
using Parser.ParseTree;

public static class InfixConversion
{
    /// <summary>
    /// Builds expression assuming the top production has format:
    /// nonTerminal -> (expression, operator, expression, operator, ..., operator, expression)
    /// </summary>
    public static Expression ToInfix(this IParseTree<Symbol> node)
    {
        var children = node.Children;
        Debug.Assert(children.Count % 2 == 1);
        var firstExpression = children[0].ToExpression();
        var pairsOperatorExpression = children.Skip(1).Chunk(2)
            .Select(pair => (Op: pair[0].ToOperator(), Right: pair[1].ToExpression()));
        return pairsOperatorExpression.Aggregate(firstExpression,
            (left, pair) => new Infix(left, pair.Right, pair.Op));
    }

    /// <summary>
    /// Returns Infix Operator matching given ParseTree or throws an exception
    /// </summary>
    private static Infix.Op ToOperator(this IParseTree<Symbol> node) => node.Symbol switch
    {
        Terminal { Category: LexicalGrammarCategory.Operators, Text: "&&" } => Infix.Op.ScAnd,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: "||" } => Infix.Op.ScOr,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: "==" } => Infix.Op.Equals,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: ">" } => Infix.Op.Greater,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: "<" } => Infix.Op.Less,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: ">=" } => Infix.Op.GreaterOrEquals,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: "<=" } => Infix.Op.LessOrEquals,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: "+" } => Infix.Op.Plus,
        Terminal { Category: LexicalGrammarCategory.Operators, Text: "-" } => Infix.Op.Minus,
        NonTerminal { Inner: NonTerminalSymbol.LogicalOperator } => node.Children[0].ToOperator(),
        NonTerminal { Inner: NonTerminalSymbol.ComparisonOperator } => node.Children[0].ToOperator(),
        NonTerminal { Inner: NonTerminalSymbol.ArithmeticOperator } => node.Children[0].ToOperator(),
        _ => throw new ArgumentException("Invalid ParseTree for Operator")
    };
}
