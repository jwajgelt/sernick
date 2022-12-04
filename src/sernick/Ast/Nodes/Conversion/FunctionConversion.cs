namespace sernick.Ast.Nodes.Conversion;

using Grammar.Syntax;
using Parser.ParseTree;

public static class FunctionConversion
{
    /// <summary>
    /// Creates FunctionCall from ParseTree.
    /// Requires the top Production to be a valid functionCall production:
    /// 1. functionCall -> identifier * parOpen * functionArguments * parClose
    /// </summary>
    public static FunctionCall ToFunctionCall(this IParseTree<Symbol> node) => node switch
    {
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.FunctionCall }, Children: var children
        } when children.Count == 4
            => new FunctionCall(children[0].ToIdentifier(), children[2].ToArguments(), node.LocationRange),

        _ => throw new ArgumentException("Invalid ParseTree for FunctionCall")
    };

    /// <summary>
    /// Creates enumerable of function arguments.
    /// Requires the top Production to be a valid functionArguments production:
    /// 1. functionArguments => arg * "," * arg * "," * ...
    /// </summary>
    private static IReadOnlyCollection<Expression> ToArguments(this IParseTree<Symbol> node) => node switch
    {
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.FunctionArguments },
            Children: var children
        } => children.SkipCommas().SelectExpressions().ToList(),
        _ => throw new ArgumentException("Invalid ParseTree for FunctionArguments")
    };

    /// <summary>
    /// Creates FunctionDefinition from ParseTree.
    /// Requires the top Production to be a valid FunctionDeclaration production:
    /// 1. functionDeclaration -> funKeyword * identifier * parOpen * functionParameters * parClose * typeSpec? * codeBlock
    /// </summary>
    public static FunctionDefinition ToFunctionDefinition(this IParseTree<Symbol> node)
    {
        return node switch
        {
            // 1. functionDeclaration -> funKeyword * identifier * parOpen * functionParameters * parClose * codeBlock
            {
                Symbol: NonTerminal { Inner: NonTerminalSymbol.FunctionDeclaration }, Children: var children
            } when children.Count == 6
                => new FunctionDefinition(children[1].ToIdentifier(),
                    children[3].ToFunctionParameters(),
                    new UnitType(),
                    children[5].ToCodeBlock(),
                    node.LocationRange),

            // 2. functionDeclaration -> funKeyword * identifier * parOpen * functionParameters * parClose * typeSpec * codeBlock
            {
                Symbol: NonTerminal { Inner: NonTerminalSymbol.FunctionDeclaration }, Children: var children
            } when children.Count == 7
                => new FunctionDefinition(children[1].ToIdentifier(),
                    children[3].ToFunctionParameters(),
                    children[5].ToType(),
                    children[6].ToCodeBlock(),
                    node.LocationRange),
            _ => throw new ArgumentException("Invalid ParseTree for FunctionDefinition")
        };
    }

    /// <summary>
    /// Creates FunctionParameterDeclaration from ParseTree.
    /// Requires the top Production to be a valid FunctionParameter or functionParameterWithDefault production:
    /// 1. functionParameter -> identifier * typeSpec
    /// 2. functionParameterDeclarationDefVal -> functionParameterDeclaration * assignOperator * literalValue
    /// </summary>
    public static FunctionParameterDeclaration ToFunctionParameterDeclaration(this IParseTree<Symbol> node) =>
        node switch
        {
            // FunctionParameter
            // 1. functionParameter -> identifier * typeSpec
            {
                Symbol: NonTerminal { Inner: NonTerminalSymbol.FunctionParameter }, Children: var children
            } when children.Count == 2
                => new FunctionParameterDeclaration(children[0].ToIdentifier(), children[1].ToType(), null,
                    node.LocationRange),

            // FunctionParameterWithDefaultValue
            // 2. functionParameterDeclarationDefVal -> identifier * typeSpec * assignOperator * literalValue
            {
                Symbol: NonTerminal { Inner: NonTerminalSymbol.FunctionParameterWithDefaultValue }, Children: var children
            } when children.Count == 4
                => new FunctionParameterDeclaration(children[0].ToIdentifier(), children[1].ToType(), children[3].ToLiteral(),
                    node.LocationRange),

            _ => throw new ArgumentException("Invalid ParseTree for FunctionParameterDefinition")
        };

    /// <summary>
    /// Creates list of FunctionParameterDeclaration from ParseTree.
    /// Requires the top Production to be a valid functionParameters production:
    /// 1. functionParameters -> param * "," * param * "," * ...
    /// </summary>
    private static IReadOnlyCollection<FunctionParameterDeclaration> ToFunctionParameters(this IParseTree<Symbol> node) =>
        node switch
        {
            { Symbol: NonTerminal { Inner: NonTerminalSymbol.FunctionParameters }, Children: var children }
                => children
                    .SkipCommas()
                    .Select(param => param.ToFunctionParameterDeclaration())
                    .ToList(),

            _ => throw new ArgumentException("Invalid ParseTree for FunctionParameterDefinition")
        };
}
