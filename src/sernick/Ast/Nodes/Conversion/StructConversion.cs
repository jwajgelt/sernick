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
}
