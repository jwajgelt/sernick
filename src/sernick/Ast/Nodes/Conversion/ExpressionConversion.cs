namespace sernick.Ast.Nodes.Conversion;

using Grammar.Lexicon;
using Grammar.Syntax;
using Parser.ParseTree;

public static class ExpressionConversion
{
    public static Expression ToExpression(this IParseTree<Symbol> node)
    {
        // Delegate to appropriate conversion method. 
        // We use switch statements with cases for unhandled symbols
        // so that it is easy to see which categories are handled. 
        switch (node.Symbol)
        {
            // Terminal Symbols
            case Terminal terminal:
                switch (terminal.Category)
                {
                    case LexicalGrammarCategory.Literals:
                        return node.ToLiteral();
                    case LexicalGrammarCategory.Keywords:
                        return node.ToKeyword();
                    case LexicalGrammarCategory.VariableIdentifiers:
                        return node.ToVariableValue();

                    // Following Categories can't be converted to an expression without any context
                    case LexicalGrammarCategory.BracesAndParentheses:
                    case LexicalGrammarCategory.Colon:
                    case LexicalGrammarCategory.Comma:
                    case LexicalGrammarCategory.Comments:
                    case LexicalGrammarCategory.Operators:
                    case LexicalGrammarCategory.Semicolon:
                    case LexicalGrammarCategory.TypeIdentifiers:
                    case LexicalGrammarCategory.Whitespaces:
                        throw new ArgumentException("ParseTree can't be converted to an expression");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(node));
                }

            // NonTerminal Symbols
            case NonTerminal nonTerminal:
                switch (nonTerminal.Inner)
                {
                    // Starter symbols
                    case NonTerminalSymbol.Start:
                    case NonTerminalSymbol.Program:
                        return node.ToProgramEGetExpression();

                    case NonTerminalSymbol.ExpressionSeq:
                        return node.ToExpressionSeq();
                    case NonTerminalSymbol.ReturnExpression:
                        return node.ToReturnStatement();

                    case NonTerminalSymbol.OpenExpression:
                    case NonTerminalSymbol.LogicalOperand:
                    case NonTerminalSymbol.ComparisonOperand:
                    case NonTerminalSymbol.ArithmeticOperand:
                        return node.ToInfix();

                    case NonTerminalSymbol.SimpleExpression:
                        return node.ToSimpleExpression();
                    case NonTerminalSymbol.LiteralValue:
                        return node.ToLiteral();
                    case NonTerminalSymbol.Assignment:
                        return node.ToAssigment();

                    // Control Flow Statements
                    case NonTerminalSymbol.CodeBlock:
                        return node.ToCodeBlock();
                    case NonTerminalSymbol.CodeGroup:
                        return node.ConvertCodeGroup();
                    case NonTerminalSymbol.IfCondition:
                        return node.ToIfCondition();
                    case NonTerminalSymbol.IfExpression:
                        return node.ToIfStatement();
                    case NonTerminalSymbol.LoopExpression:
                        return node.ToLoop();

                    case NonTerminalSymbol.VariableDeclaration:
                        return node.ToVariableDeclaration();

                    // Function Expressions
                    case NonTerminalSymbol.FunctionCall:
                        return node.ToFunctionCall();
                    case NonTerminalSymbol.FunctionDeclaration:
                        return node.ToFunctionDefinition();
                    case NonTerminalSymbol.FunctionParameter:
                    case NonTerminalSymbol.FunctionParameterWithDefaultValue:
                        return node.ToFunctionParameterDeclaration();

                    // Following expression can't be converted to AST node without more context
                    case NonTerminalSymbol.ArithmeticOperator:
                    case NonTerminalSymbol.ComparisonOperator:
                    case NonTerminalSymbol.FunctionArguments:
                    case NonTerminalSymbol.FunctionParameters:
                    case NonTerminalSymbol.LogicalOperator:
                    case NonTerminalSymbol.Modifier:
                    case NonTerminalSymbol.TypeSpecification:
                        throw new ArgumentException("ParseTree can't be converted to an expression");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(node));
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
    }
}
