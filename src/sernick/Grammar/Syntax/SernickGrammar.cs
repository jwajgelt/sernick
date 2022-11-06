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
        var codeBlock = new NonTerminal(NonTerminalSymbol.CodeBlock);
        var ifStatement = new NonTerminal(NonTerminalSymbol.IfStatement);
        var loopStatement = new NonTerminal(NonTerminalSymbol.LoopStatement);
        var elseBlock = new NonTerminal(NonTerminalSymbol.ElseBlock);
        var functionCall = new NonTerminal(NonTerminalSymbol.FunctionCall);
        var assignment = new NonTerminal(NonTerminalSymbol.Assignment);
        _ = new NonTerminal(NonTerminalSymbol.VariableDeclaration);

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
        var varKeyword = new Terminal(LexicalCategory.Keywords, "var");
        var constKeyword = new Terminal(LexicalCategory.Keywords, "const");
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
        var regCodeBlock = Regex.Atom(codeBlock);
        var regElseBlock = Regex.Atom(elseBlock);

        // For terminal
        var regSemicolon = Regex.Atom(semicolon);
        var regComma = Regex.Atom(comma);
        var regColon = Regex.Atom(colon);
        var regBracesOpen = Regex.Atom(bracesOpen);
        var regBracesClose = Regex.Atom(bracesClose);
        var regParenthesesOpen = Regex.Atom(parenthesesOpen);
        var regParenthesesClose = Regex.Atom(parenthesesClose);
        var regLoopKeyword = Regex.Atom(loopKeyword);
        var regIfKeyword = Regex.Atom(ifKeyword);
        var regElseKeyword = Regex.Atom(elseKeyword);
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
            Regex.Concat(new Regex[] { regExpression, regSemicolon, regExpression })
        ));

        // Production for code block
        productions.Add(new Production<Symbol>(
            codeBlock,
            Regex.Concat(new Regex[] { regBracesOpen, regExpression, regBracesClose })
        ));

        // Production for loop (do we want to take break/return into account here?)
        productions.Add(new Production<Symbol>(
            loopStatement,
            Regex.Concat(new Regex[] { regLoopKeyword, regCodeBlock })
        ));

        // Production for if statement
        productions.Add(new Production<Symbol>(
            ifStatement,
            Regex.Concat(new Regex[] { regIfKeyword, regParenthesesOpen, regExpression, regParenthesesClose, regCodeBlock, regElseBlock })
        ));

        // Empty - else block is optional
        productions.Add(new Production<Symbol>(
            elseBlock,
            Regex.Epsilon
        ));

        productions.Add(new Production<Symbol>(
            elseBlock,
            Regex.Concat(new Regex[] { regElseKeyword, regCodeBlock })
        ));

        // Preparing regexes for function call production
        Regex argStarred = Regex.Star(Regex.Concat(new Regex[] { regExpression, regComma }));
        Regex regArgList = Regex.Union(Regex.Concat(argStarred, regExpression), Regex.Epsilon);

        productions.Add(new Production<Symbol>(
            functionCall,
            Regex.Concat(new Regex[] { regIdentifier, regParenthesesOpen, regArgList, regParenthesesClose })
        ));

        // Production for assignment
        productions.Add(new Production<Symbol>(
            assignment,
            Regex.Concat(new Regex[] { regIdentifier, regAssignmentOperator, regExpression })
        ));

        // Production for equality test
        productions.Add(new Production<Symbol>(
            assignment,
            Regex.Concat(new Regex[] { regExpression, regEqualsOperator, regExpression })
        ));

        // Production for "variable : Type"
        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regIdentifier, regColon, regTypeIdentifier })
        ));

        // +,-, &&, || operations

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regPlusOperator, regExpression })
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regMinusOperator, regExpression })
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regShortCircuitAndOperator, regExpression })
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regShortCircuitOrOperator, regExpression })
        ));


        // Comparisons: >, <, >=, <=
        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regLessOperator, regExpression })
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regGreaterOperator, regExpression })
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regGreaterOrEqualOperator, regExpression })
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regLessOrEqualOperator, regExpression })
        ));

        return new Grammar<Symbol>(
            new NonTerminal(NonTerminalSymbol.Program),
            productions
        );
    }
}
