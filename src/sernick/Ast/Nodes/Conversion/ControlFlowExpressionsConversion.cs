namespace sernick.Ast.Nodes.Conversion;

using Grammar.Syntax;
using Parser.ParseTree;

public static class ControlFlowExpressionsConversion
{
    /// <summary>
    /// Creates CodeBlock from ParseTree,
    /// Assumes that the root used a valid CodeBlock production:
    /// 1. codeBlock -> braceOpen * statements * braceClose
    /// </summary>
    public static CodeBlock ToCodeBlock(this IParseTree<Symbol> node) => node switch
    {
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.CodeBlock }, Children: var children }
            when children.Count == 3
            => new CodeBlock(children[1].ToExpressionSeq(), node.LocationRange),
        _ => throw new ArgumentException("Invalid ParseTree for CodeBlock")
    };

    /// <summary>
    /// Creates Expression from CodeGroup ParseTree,
    /// Assumes that the root used a valid CodeGroup production:
    /// 1. codeBlock -> braceOpen * (
    ///         (aliasExpression * semicolon)
    ///         | (aliasClosedExpression * Star(aliasClosedExpression) * (openExpression | aliasClosedExpression))
    ///     ) * braceClose
    /// </summary>
    public static Expression ToCodeGroup(this IParseTree<Symbol> node) => node switch
    {
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.CodeGroup },
            Children: var children
        } => children.SkipBraces().ToExpressionJoin(node.LocationRange.End),
        _ => throw new ArgumentException("Invalid ParseTree for CodeGroup"),
    };

    /// <summary>
    /// Creates LoopStatement form ParseTree.
    /// Requires that the top production is valid loopExpression production:
    /// 1. loopExpression -> loopKeyword * codeBlock
    /// </summary>
    public static LoopStatement ToLoop(this IParseTree<Symbol> node) => node switch
    {
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.LoopExpression }, Children: var children
        } when children.Count == 2
            => new LoopStatement(children[1].ToCodeBlock(), node.LocationRange),

        _ => throw new ArgumentException("Invalid ParseTree for LoopStatement"),
    };

    /// <summary>
    /// Creates IfStatement form ParseTree.
    /// Requires that the top production is valid ifExpression production:
    /// 1. ifExpression -> ifKeyword * ifCondition * codeBlock * (elseKeyword * codeBlock)?
    /// </summary>
    public static IfStatement ToIfStatement(this IParseTree<Symbol> node) => node switch
    {
        // 1. ifKeyword * ifCondition * codeBlock
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.IfExpression }, Children: var children
        } when children.Count == 3
            => new IfStatement(children[1].ToExpression(), children[2].ToCodeBlock(), null, node.LocationRange),

        // 2. ifExpression -> ifKeyword * ifCondition * codeBlock * elseKeyword * codeBlock
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.IfExpression }, Children: var children
        } when children.Count == 5
            => new IfStatement(children[1].ToExpression(), children[2].ToCodeBlock(), children[4].ToCodeBlock(), node.LocationRange),

        _ => throw new ArgumentException("Invalid ParseTree for IfStatement"),
    };

    /// <summary>
    /// Creates IfCondition Expression form ParseTree.
    /// Requires that the top production is valid ifCondition production:
    /// 1. ifCondition -> codeGroup | (parOpen * aliasExpression * parClose)
    /// </summary>
    public static Expression ToIfCondition(this IParseTree<Symbol> node) => node switch
    {
        // 1. ifCondition -> codeGroup
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.IfCondition }, Children: var children
        } when children.Count == 1
            => children[0].ToCodeGroup(),

        // 2. ifCondition -> parOpen * expression * parClose
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.IfCondition }, Children: var children
        } when children.Count == 3 => children[1].ToExpression(),

        _ => throw new ArgumentException("Invalid ParseTree for IfCondition"),
    };
}
