namespace sernick.Ast.Nodes.Conversion;

using Grammar.Lexicon;
using Grammar.Syntax;
using Parser.ParseTree;

public static class ValuesConversion
{
    /// <summary>
    /// Converts ParseTree to a LiteralValue object.
    /// Requires that ParseTree has a terminal symbol for a Literal
    /// or its top production is a LiteralValue production.
    /// </summary>
    public static LiteralValue ToLiteral(this IParseTree<Symbol> node) => node switch
    {
        // Null Literal
        { Symbol: Terminal { Category: LexicalGrammarCategory.Literals, Text: "null" } }
            => new NullPointerLiteralValue(node.LocationRange),

        // Boolean Literals
        { Symbol: Terminal { Category: LexicalGrammarCategory.Literals, Text: "true" } } => new BoolLiteralValue(true,
            node.LocationRange),
        { Symbol: Terminal { Category: LexicalGrammarCategory.Literals, Text: "false" } } => new BoolLiteralValue(false,
            node.LocationRange),

        // Int Literal
        { Symbol: Terminal { Category: LexicalGrammarCategory.Literals, Text: var text } }
            when int.TryParse(text, out var value)
            => new IntLiteralValue(value, node.LocationRange),

        // LiteralValue
        // 1. LiteralValue -> trueLiteral | falseLiteral | digitLiteral | nullLiteral
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.LiteralValue }, Children: var children }
            when children.Count == 1 => children[0].ToLiteral(),

        _ => throw new ArgumentException("Invalid ParseTree for Literals")
    };

    /// <summary>
    /// Requires that node is a Terminal VariableValue
    /// </summary>
    public static VariableValue ToVariableValue(this IParseTree<Symbol> node) => node switch
    {
        { Symbol: Terminal { Category: LexicalGrammarCategory.VariableIdentifiers } }
            => new VariableValue(node.ToIdentifier(), node.LocationRange),
        _ => throw new ArgumentException("Invalid ParseTree for VariableValue")
    };
}
