namespace sernick.Ast.Nodes.Conversion;

using Grammar.Lexicon;
using Grammar.Syntax;
using Input;
using Parser.ParseTree;
using Utility;

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
            { Symbol: Terminal { Text: var name, Category: LexicalGrammarCategory.TypeIdentifiers } } =>
                throw new UnknownTypeException(name, node.LocationRange),

            // TypeSpecification
            // typeSpec -> colon * typeIdentifier
            {
                Symbol: NonTerminal { Inner: NonTerminalSymbol.TypeSpecification }, Children: var children
            } when children.Count == 2 => ToType(children[1]),

            _ => throw new ArgumentException("Invalid ParseTree for Type")
        };
    }
}

public sealed class UnknownTypeException : Exception
{
    public string Name { get; }
    public Range<ILocation> LocationRange { get; }

    public UnknownTypeException(string name, Range<ILocation> locationRange) : base($"Unknown type name: {name}") =>
        (Name, LocationRange) = (name, locationRange);
}
