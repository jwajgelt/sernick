namespace sernick.Ast.Nodes.Conversion;

using Grammar.Lexicon;
using Grammar.Syntax;
using Parser.ParseTree;

public static class TypeConversion
{
    /// <summary>
    /// Converts ParseTree to a Type object.
    /// Requires that ParseTree has a terminal symbol for a Type
    /// or its top production is a typeSpec production.
    /// </summary>
    public static Type ToType(this IParseTree<Symbol> node)
    {
        return node switch
        {
            // Terminal Types
            { Symbol: Terminal { Text: "Int", Category: LexicalGrammarCategory.TypeIdentifiers } } => new IntType(),
            { Symbol: Terminal { Text: "Bool", Category: LexicalGrammarCategory.TypeIdentifiers } } => new BoolType(),
            { Symbol: Terminal { Text: "Unit", Category: LexicalGrammarCategory.TypeIdentifiers } } => new UnitType(),

            // TypeSpecification
            // typeSpec -> colon * typeIdentifier
            {
                Symbol: NonTerminal { Inner: NonTerminalSymbol.TypeSpecification }, Children: var children
            } when children.Count == 2 => ToType(children[1]),

            _ => throw new ArgumentException("Invalid ParseTree for Type")
        };
    }
}
