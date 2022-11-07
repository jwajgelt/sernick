namespace sernick.Grammar.Syntax;

using LexicalCategory = Lexicon.LexicalGrammarCategoryType;
using Regex = Common.Regex.Regex<Symbol>;

public static class SernickGrammar
{
    public static Grammar<Symbol> Create()
    {
        var productions = new List<Production<Symbol>>();

        // Symbols of our grammar

        // Non-terminal
        var program = new NonTerminal(NonTerminalSymbol.Program);
        var expression = new NonTerminal(NonTerminalSymbol.Expression);
        var joinableExpression = new NonTerminal(NonTerminalSymbol.JoinableExpression);
        var codeBlock = new NonTerminal(NonTerminalSymbol.CodeBlock);
        var ifStatement = new NonTerminal(NonTerminalSymbol.IfStatement);
        var loopStatement = new NonTerminal(NonTerminalSymbol.LoopStatement);
        var expressionWithReturn = new NonTerminal(NonTerminalSymbol.ExpressionContainingReturn);
        var expressionMaybeWithReturn = new NonTerminal(NonTerminalSymbol.ExpressionMaybeContainingReturn);
        var elseBlock = new NonTerminal(NonTerminalSymbol.ElseBlock);
        var functionCall = new NonTerminal(NonTerminalSymbol.FunctionCall);
        var assignment = new NonTerminal(NonTerminalSymbol.Assignment);
        var variableDeclaration = new NonTerminal(NonTerminalSymbol.VariableDeclaration);
        var functionDefinition = new NonTerminal(NonTerminalSymbol.FunctionDefinition);

        // Terminal
        var semicolon = new Terminal(LexicalCategory.Semicolon, ";");
        var comma = new Terminal(LexicalCategory.Comma, ",");
        var colon = new Terminal(LexicalCategory.Colon, ":");
        var bracesOpen = new Terminal(LexicalCategory.BracesAndParentheses, "{");
        var bracesClose = new Terminal(LexicalCategory.BracesAndParentheses, "}");
        var parenthesesOpen = new Terminal(LexicalCategory.BracesAndParentheses, "(");
        var parenthesesClose = new Terminal(LexicalCategory.BracesAndParentheses, ")");

        var loopKeyword = new Terminal(LexicalCategory.Keywords, "loop");
        var ifKeyword = new Terminal(LexicalCategory.Keywords, "if");
        var elseKeyword = new Terminal(LexicalCategory.Keywords, "else");
        var breakKeyword = new Terminal(LexicalCategory.Keywords, "break");
        var continueKeyword = new Terminal(LexicalCategory.Keywords, "continue");
        var returnKeyword = new Terminal(LexicalCategory.Keywords, "return");
        var varKeyword = new Terminal(LexicalCategory.Keywords, "var");
        var constKeyword = new Terminal(LexicalCategory.Keywords, "const");
        var funKeyword = new Terminal(LexicalCategory.Keywords, "fun");

        var trueLiteral = new Terminal(LexicalCategory.Literals, "true");
        var falseLiteral = new Terminal(LexicalCategory.Literals, "false");
        var digitLiteral = new Terminal(LexicalCategory.Literals, "");

        var identifier = new Terminal(LexicalCategory.VariableIdentifiers, "");
        var typeIdentifier = new Terminal(LexicalCategory.TypeIdentifiers, "");

        var assignmentOperator = new Terminal(LexicalCategory.Operators, "=");
        var equalsOperator = new Terminal(LexicalCategory.Operators, "==");
        var plusOperator = new Terminal(LexicalCategory.Operators, "+");
        var minusOperator = new Terminal(LexicalCategory.Operators, "-");
        var shortCircuitAndOperator = new Terminal(LexicalCategory.Operators, "&&");
        var shortCircuitOrOperator = new Terminal(LexicalCategory.Operators, "||");
        var greaterOperator = new Terminal(LexicalCategory.Operators, ">");
        var lessOperator = new Terminal(LexicalCategory.Operators, "<");
        var greaterOrEqualOperator = new Terminal(LexicalCategory.Operators, ">=");
        var lessOrEqualOperator = new Terminal(LexicalCategory.Operators, "<=");

        // Atomic regular expressions representing symbols

        // For non-terminal
        var regExpression = Regex.Atom(expression);
        var regJoinableExpression = Regex.Atom(joinableExpression);
        var regCodeBlock = Regex.Atom(codeBlock);
        var regElseBlock = Regex.Atom(elseBlock);
        var regExpressionWithReturn = Regex.Atom(expressionWithReturn);
        var regExpressionMaybeWithReturn = Regex.Atom(expressionMaybeWithReturn);
        var regIfStatement = Regex.Atom(ifStatement);
        var regLoopStatement = Regex.Atom(loopStatement);
        var regVarDeclaration = Regex.Atom(variableDeclaration);
        var regFunctionDefinition = Regex.Atom(functionDefinition);
        var regAssignment = Regex.Atom(assignment);

        // For terminal
        var regSemicolon = Regex.Atom(semicolon);
        var regComma = Regex.Atom(comma);
        var regColon = Regex.Atom(colon);
        var regBracesOpen = Regex.Atom(bracesOpen);
        var regBracesClose = Regex.Atom(bracesClose);
        var regParenthesesOpen = Regex.Atom(parenthesesOpen);
        var regParenthesesClose = Regex.Atom(parenthesesClose);

        var regLoopKeyword = Regex.Atom(loopKeyword);
        var regFunKeyword = Regex.Atom(funKeyword);
        var regIfKeyword = Regex.Atom(ifKeyword);
        var regElseKeyword = Regex.Atom(elseKeyword);
        var regVarKeyword = Regex.Atom(varKeyword);
        var regBreakKeyword = Regex.Atom(breakKeyword);
        var regConstKeyword = Regex.Atom(constKeyword);
        var regContinueKeyword = Regex.Atom(continueKeyword);
        var regReturnKeyword = Regex.Atom(returnKeyword);

        var regTrueLiteral = Regex.Atom(trueLiteral);
        var regFalseLiteral = Regex.Atom(falseLiteral);
        var regDigitLiteral = Regex.Atom(digitLiteral);
        var regIdentifier = Regex.Atom(identifier);
        var regTypeIdentifier = Regex.Atom(typeIdentifier);
        var regAssignmentOperator = Regex.Atom(assignmentOperator);
        var regEqualsOperator = Regex.Atom(equalsOperator);
        var regPlusOperator = Regex.Atom(plusOperator);
        var regMinusOperator = Regex.Atom(minusOperator);
        var regShortCircuitAndOperator = Regex.Atom(shortCircuitAndOperator);
        var regShortCircuitOrOperator = Regex.Atom(shortCircuitOrOperator);
        var regGreaterOperator = Regex.Atom(greaterOperator);
        var regLessOperator = Regex.Atom(lessOperator);
        var regGreaterOrEqualOperator = Regex.Atom(greaterOrEqualOperator);
        var regLessOrEqualOperator = Regex.Atom(lessOrEqualOperator);

        // Production: the whole program can be seen as an expression
        productions.Add(new Production<Symbol>(
            program,
            regExpression
        ));

        // Expression can be a join of two expressions
        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(Regex.Concat(Regex.Star(regJoinableExpression), regJoinableExpression), regExpression)
        ));

        productions.Add(new Production<Symbol>(
            expression,
            regJoinableExpression
        ));

        productions.Add(new Production<Symbol>(
            joinableExpression,
            Regex.Union(
                regLoopStatement,
                regIfStatement,
                regFunctionDefinition
            )
        ));

        productions.Add(new Production<Symbol>(
            joinableExpression,
            Regex.Concat(regExpression, regSemicolon)
        ));

        // Production for code block
        productions.Add(new Production<Symbol>(
            codeBlock,
            Regex.Concat(regBracesOpen, regExpression, regBracesClose)
        ));

        // All non-terminals that can be directly "casted" to expression
        productions.Add(new Production<Symbol>(
            expression,
            Regex.Union(regCodeBlock,
                        regExpressionWithReturn,
                        regExpressionMaybeWithReturn,
                        regVarDeclaration,
                        regAssignment
            )
        ));

        // Production for loop (taking continue/return/break into account)

        // (1) for simplicity, let's create a special "code block" -- for expressions inside loop, which must contain "return"
        productions.Add(new Production<Symbol>(
            loopStatement,
            Regex.Concat(regLoopKeyword, regBracesOpen, regExpressionWithReturn, regBracesClose)
        ));

        // (2) we should have a return somewhere inside (at least one, but maybe more)
        productions.Add(new Production<Symbol>(
            expressionWithReturn,
            Regex.Concat(regExpressionMaybeWithReturn, regReturnKeyword, regExpressionMaybeWithReturn)
        ));

        // (3) we may or may not have a "break/continue" or more "return"s
        productions.Add(new Production<Symbol>(
            expressionMaybeWithReturn,
            Regex.Union(
                Regex.Concat(regExpressionMaybeWithReturn, regBreakKeyword, regSemicolon),
                Regex.Concat(regExpressionMaybeWithReturn, regContinueKeyword, regSemicolon),
                Regex.Concat(regExpressionMaybeWithReturn, regReturnKeyword, regSemicolon), // return nothing
                Regex.Concat(regExpressionMaybeWithReturn, regReturnKeyword, regExpression), // return expression
                Regex.Concat(regExpressionMaybeWithReturn, regExpression),
                Regex.Epsilon
            )
        ));

        // Production for if statement
        productions.Add(new Production<Symbol>(
            ifStatement,
            Regex.Concat(regIfKeyword, regParenthesesOpen, regExpression, regParenthesesClose, regCodeBlock, regElseBlock)
        ));

        // Empty - else block is optional
        productions.Add(new Production<Symbol>(
            elseBlock,
            Regex.Epsilon
        ));

        productions.Add(new Production<Symbol>(
            elseBlock,
            Regex.Concat(regElseKeyword, regCodeBlock)
        ));

        // Production for "variable : Type"
        var regIdentifierWithType = Regex.Concat(regIdentifier, regColon, regTypeIdentifier);
        var identifierWithTypeStarred = Regex.Star(Regex.Concat(regIdentifierWithType, regComma));
        var regArgDeclList = Regex.Concat(identifierWithTypeStarred, regIdentifierWithType);

        // function declaration
        productions.Add(new Production<Symbol>(
            functionDefinition,
            Regex.Concat(regFunKeyword, regIdentifier, regParenthesesOpen, regArgDeclList, regParenthesesClose, regCodeBlock)
        ));

        // function call
        var argStarred = Regex.Star(Regex.Concat(regExpression, regComma));
        var regArgList = Regex.Union(Regex.Concat(argStarred, regExpression), Regex.Epsilon);

        productions.Add(new Production<Symbol>(
            functionCall,
            Regex.Concat(regIdentifier, regParenthesesOpen, regArgList, regParenthesesClose)
        ));

        // Production for assignment
        var regUntypedAssignment = Regex.Concat(regIdentifier, regAssignmentOperator, regExpression);
        var regTypedAssignment = Regex.Concat(regIdentifierWithType, regAssignmentOperator, regExpression);

        productions.Add(new Production<Symbol>(
            assignment,
            regUntypedAssignment
        ));

        // variable declaration 
        productions.Add(new Production<Symbol>(
            variableDeclaration,
            Regex.Union(
                // both with var or const, we may or may not specify a type
                Regex.Concat(
                    Regex.Union(regVarKeyword, regConstKeyword),
                    Regex.Union(regUntypedAssignment, regTypedAssignment)
                )
            )
        ));

        // equality test
        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regEqualsOperator, regExpression)
        ));

        // +,-, &&, || operations

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regPlusOperator, regExpression)
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regMinusOperator, regExpression)
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regShortCircuitAndOperator, regExpression)
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regShortCircuitOrOperator, regExpression)
        ));

        // Comparisons: >, <, >=, <=
        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regLessOperator, regExpression)
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regGreaterOperator, regExpression)
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regGreaterOrEqualOperator, regExpression)
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(regExpression, regLessOrEqualOperator, regExpression)
        ));

        // expression can be a literal
        productions.Add(new Production<Symbol>(
            expression,
            Regex.Union(regTrueLiteral, regFalseLiteral, regDigitLiteral)
        ));

        return new Grammar<Symbol>(
            new NonTerminal(NonTerminalSymbol.Program),
            productions
        );
    }
}
