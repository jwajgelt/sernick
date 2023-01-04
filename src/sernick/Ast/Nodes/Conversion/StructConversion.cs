namespace sernick.Ast.Nodes.Conversion;

using Grammar.Syntax;
using Parser.ParseTree;

public static class StructConversion
{
    /// <summary>
    /// Creates StructDeclaration from ParseTree.
    /// Requires the top Production to be a valid StructDeclaration production:
    /// 1. structDeclaration -> structKeyword * typeIdentifier * braceOpen * structDeclarationFields * braceClose
    /// </summary>
    public static StructDeclaration ToStructDeclaration(this IParseTree<Symbol> node) =>
        node switch
        {
            { Symbol: NonTerminal { Inner: NonTerminalSymbol.StructDeclaration }, Children: var children } =>
                new StructDeclaration(children[1].ToIdentifier(), children[3].ToStructDeclarationFields(), node.LocationRange),

            _ => throw new ArgumentException("Invalid ParseTree for StructDeclaration")
        };

    /// <summary>
    /// Creates StructValue from ParseTree.
    /// Requires the top Production to be a valid StructValue production:
    /// 1. structValue -> typeIdentifier * braceOpen * structValueFields * braceClose
    /// </summary>
    public static StructValue ToStructValue(this IParseTree<Symbol> node)
    {
        return node switch
        {
            { Symbol: NonTerminal { Inner: NonTerminalSymbol.StructValue }, Children: var children } =>
                new StructValue(children[0].ToIdentifier(), children[2].ToStructValueFields(), node.LocationRange),

            _ => throw new ArgumentException("Invalid ParseTree for StructDeclaration")
        };
    }

    private static IReadOnlyCollection<FieldDeclaration> ToStructDeclarationFields(this IParseTree<Symbol> node) =>
        node switch
        {
            // structDeclarationFields -> [ structFieldDeclaration (, structFieldDeclaration)^ ]
            { Symbol: NonTerminal { Inner: NonTerminalSymbol.StructDeclarationFields }, Children: var children } =>
                children.SkipCommas().Select(field => field.ToStructFieldDeclaration()).ToList(),

            _ => throw new ArgumentException("Invalid ParseTree for StructDeclarationFields")
        };

    private static FieldDeclaration ToStructFieldDeclaration(this IParseTree<Symbol> node) =>
        node switch
        {
            // structFieldDeclaration -> identifier * typeSpec
            { Symbol: NonTerminal { Inner: NonTerminalSymbol.StructFieldDeclaration }, Children: var children }
                when children.Count == 2
                => new FieldDeclaration(children[0].ToIdentifier(), children[1].ToType(), node.LocationRange),

            _ => throw new ArgumentException("Invalid ParseTree for StructFieldDeclaration")
        };

    private static IReadOnlyCollection<StructFieldInitializer> ToStructValueFields(this IParseTree<Symbol> node) =>
        node switch
        {
            // structValueFields -> [ structFieldInitializer (, structFieldInitializer)^ ]
            { Symbol: NonTerminal { Inner: NonTerminalSymbol.StructValueFields }, Children: var children } =>
                children.SkipCommas().Select(field => field.ToStructFieldInitializer()).ToList(),

            _ => throw new ArgumentException("Invalid ParseTree for StructValueFields")
        };

    private static StructFieldInitializer ToStructFieldInitializer(this IParseTree<Symbol> node) =>
        node switch
        {
            // structFieldInitializer -> identifier : expression
            { Symbol: NonTerminal { Inner: NonTerminalSymbol.StructFieldInitializer }, Children: var children }
                when children.Count == 3
                => new StructFieldInitializer(children[0].ToIdentifier(), children[2].ToExpression(), node.LocationRange),

            _ => throw new ArgumentException("Invalid ParseTree for StructFieldInitializer")
        };
}
