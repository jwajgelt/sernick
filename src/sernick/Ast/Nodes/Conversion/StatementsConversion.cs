namespace sernick.Ast.Nodes.Conversion;

using Grammar.Lexicon;
using Grammar.Syntax;
using Input;
using Parser.ParseTree;

public static class StatementsConversion
{
    /// <summary>
    /// Requires that the top production is of type:
    /// 1. start -> program
    /// 2. program -> expressionSeq
    /// </summary>
    public static Expression ToProgramExpression(this IParseTree<Symbol> node) => node.Children.Count switch
    {
        1 => new FunctionDefinition(
            Name: new Identifier("", (node.LocationRange.Start, node.LocationRange.Start)),
            Parameters: Array.Empty<FunctionParameterDeclaration>(),
            Body: new CodeBlock(node.Children[0].ToExpression(), node.LocationRange),
            ReturnType: new UnitType(),
            LocationRange: node.LocationRange
            ),
        _ => throw new ArgumentException("Expected single child")
    };

    /// <summary>
    /// Joins list of expressions.
    /// If it ends with a semicolon, then adds a special EmptyExpression at the end.
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="endLocation">
    /// End location of the fragment,
    /// it is used to create a EmptyExpression if necessary
    /// </param>
    /// <returns></returns>
    public static Expression ToExpressionJoin(this IEnumerable<IParseTree<Symbol>> nodes, ILocation endLocation)
    {
        var nodesList = nodes.ToList();
        var isSemicolon = nodesList.LastOrDefault()?.IsSemicolon() ?? false;

        var expressions = nodesList.SkipSemicolons().SelectExpressions().ToList();

        if (isSemicolon || !expressions.Any())
        {
            // If list of statements is empty or ends with a semicolon, insert a unit expression at the end.
            // The joined list will have a Unit type
            expressions.Add(new EmptyExpression((endLocation, endLocation)));
        }

        return expressions.Join();
    }

    /// <summary>
    /// Requires that the top production is of type:
    /// 1. expressionSeq -> Star(aliasClosedExpression) * openExpression?
    /// Returns noop expression when empty.
    /// </summary>
    public static Expression ToExpressionSeq(this IParseTree<Symbol> node) => node switch
    {
        {
            Symbol: NonTerminal { Inner: NonTerminalSymbol.ExpressionSeq },
            Children: var children
        } => children.ToExpressionJoin(node.LocationRange.End),
        _ => throw new ArgumentException("Invalid ParseTree for ExpressionSeq")
    };

    /// <summary>
    /// Requires that the top production is of type:
    /// 1. openExpression -> variableDeclaration | breakKeyword | continueKeyword | returnExpression
    /// 2. openExpression -> assignmentOperand [ = expression ]
    /// </summary>
    public static Expression ToOpenExpression(this IParseTree<Symbol> node) => node switch
    {
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.OpenExpression }, Children: var children }
            when children.Count == 1 => children[0].ToExpression(),

        { Symbol: NonTerminal { Inner: NonTerminalSymbol.OpenExpression }, Children: var children }
            when children.Count == 3
            => new Assignment(children[0].ToExpression(), children[2].ToExpression(), node.LocationRange),

        _ => throw new ArgumentException("Invalid ParseTree for OpenExpression")
    };

    /// <summary>
    /// Requires that the top production is of type:
    /// 1. simpleExpression -> literalValue
    /// 2. simpleExpression -> parOpen * aliasExpression * parClose
    /// 3. simpleExpression -> identifier | functionCall
    /// </summary>
    public static Expression ToSimpleExpression(this IParseTree<Symbol> node) => node switch
    {
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.SimpleExpression }, Children: var children }
            when children.Count == 1
            => children[0].ToExpression(),

        // 2. simpleExpression -> parOpen * aliasExpression * parClose
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.SimpleExpression }, Children: var children }
            when children.Count == 3 => children[1].ToExpression(),
        _ => throw new ArgumentException("Invalid ParseTree for SimpleExpression")
    };

    /// <summary>
    /// Requires that the top production is of type:
    /// 1. returnExpression -> returnKeyword * aliasExpression?
    /// </summary>
    public static ReturnStatement ToReturnStatement(this IParseTree<Symbol> node) => node switch
    {
        // 1. returnExpression -> returnKeyword
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.ReturnExpression }, Children: var children }
            when children.Count == 1
            => new ReturnStatement(null, node.LocationRange),

        // 2. returnExpression -> returnKeyword * aliasExpression
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.ReturnExpression }, Children: var children }
            when children.Count == 2 => new ReturnStatement(children[1].ToExpression(), node.LocationRange),

        _ => throw new ArgumentException("Invalid ParseTree for ReturnStatement")
    };

    /// <summary>
    /// Requires that the top production is of type:
    /// 1. variableDeclaration -> modifier * identifier * (
    ///         | = expression
    ///         | typeSpec
    ///         | typeSpec = expression
    ///     )
    /// </summary>
    public static VariableDeclaration ToVariableDeclaration(this IParseTree<Symbol> node)
    {
        if (node is not { Symbol: NonTerminal { Inner: NonTerminalSymbol.VariableDeclaration }, Children: var children })
        {
            throw new ArgumentException("Invalid ParseTree for VariableDeclaration");
        }

        var isConst = children[0].IsConst();

        switch (children.Count)
        {
            case 4:
                {
                    // 1. variableDeclaration -> modifier identifier = expression
                    var name = children[1].ToIdentifier();
                    var expression = children[3].ToExpression();
                    return new VariableDeclaration(name, null, expression, isConst, node.LocationRange);
                }
            case 3:
                {
                    // 2. variableDeclaration -> modifier identifier typeSpec
                    var name = children[1].ToIdentifier();
                    var type = children[2].ToType();
                    return new VariableDeclaration(name, type, null, isConst, node.LocationRange);
                }
            case 5:
                {
                    // 3. variableDeclaration -> modifier identifier typeSpec = expression
                    var name = children[1].ToIdentifier();
                    var type = children[2].ToType();
                    var expression = children[4].ToExpression();
                    return new VariableDeclaration(name, type, expression, isConst, node.LocationRange);
                }
            default:
                throw new ArgumentException("Invalid ParseTree for VariableDeclaration");
        }
    }

    /// <returns>
    /// "true" if node represents a "const" modifier or "false" if represents a "var" modifier
    /// </returns>
    /// <exception cref="ArgumentException">If ParseTree doesn't represent a modifier</exception>
    private static bool IsConst(this IParseTree<Symbol> node) => node switch
    {
        { Symbol: NonTerminal { Inner: NonTerminalSymbol.Modifier }, Children: var children } when children.Count == 1
            => children[0] switch
            {
                // 1. modifier -> constKeyword
                { Symbol: Terminal { Text: "const", Category: LexicalGrammarCategory.Keywords } } => true,
                // 2. modifier -> varKeyword
                { Symbol: Terminal { Text: "var", Category: LexicalGrammarCategory.Keywords } } => false,
                _ => throw new ArgumentException("Invalid ParseTree for a variable modifier")
            },
        _ => throw new ArgumentException("Invalid ParseTree for a variable modifier")
    };
}
