namespace sernick.Grammar.Syntax;

using System.Diagnostics;
using Common.Regex;
using static Common.Regex.Regex<Symbol>;
using LexicalCategory = Lexicon.LexicalGrammarCategory;
using Production = Production<Symbol>;
using Regex = Common.Regex.Regex<Symbol>;

public static class SernickGrammar
{
    public static Grammar<Symbol> Create()
    {
        var productions = new List<Production>();

        // Symbols of our grammar

        // Non-terminal
        var program = Symbol.Of(NonTerminalSymbol.Program);
        var expressionSeq = Atom(Symbol.Of(NonTerminalSymbol.ExpressionSeq));
        var openExpression = Atom(Symbol.Of(NonTerminalSymbol.OpenExpression)); // requires semicolon
        var codeBlock = Atom(Symbol.Of(NonTerminalSymbol.CodeBlock)); // {}
        var codeGroup = Atom(Symbol.Of(NonTerminalSymbol.CodeGroup)); // ()
        var returnExpression = Atom(Symbol.Of(NonTerminalSymbol.ReturnExpression));
        var logicalOperand = Atom(Symbol.Of(NonTerminalSymbol.LogicalOperand));
        var logicalOperator = Atom(Symbol.Of(NonTerminalSymbol.LogicalOperator));
        var comparisonOperand = Atom(Symbol.Of(NonTerminalSymbol.ComparisonOperand));
        var comparisonOperator = Atom(Symbol.Of(NonTerminalSymbol.ComparisonOperator));
        var arithmeticOperand = Atom(Symbol.Of(NonTerminalSymbol.ArithmeticOperand));
        var arithmeticOperator = Atom(Symbol.Of(NonTerminalSymbol.ArithmeticOperator));
        var simpleExpression = Atom(Symbol.Of(NonTerminalSymbol.SimpleExpression)); // (E) or x or f() or 5
        var literalValue = Atom(Symbol.Of(NonTerminalSymbol.LiteralValue));
        var functionCall = Atom(Symbol.Of(NonTerminalSymbol.FunctionCall));
        var functionArguments = Atom(Symbol.Of(NonTerminalSymbol.FunctionArguments));
        var assignment = Atom(Symbol.Of(NonTerminalSymbol.Assignment));
        var ifCondition = Atom(Symbol.Of(NonTerminalSymbol.IfCondition));
        var ifExpression = Atom(Symbol.Of(NonTerminalSymbol.IfExpression));
        var loopExpression = Atom(Symbol.Of(NonTerminalSymbol.LoopExpression));
        var modifier = Atom(Symbol.Of(NonTerminalSymbol.Modifier)); // var or const
        var typeSpec = Atom(Symbol.Of(NonTerminalSymbol.TypeSpecification)); // ": Type"
        var variableDeclaration = Atom(Symbol.Of(NonTerminalSymbol.VariableDeclaration));
        var functionDeclaration = Atom(Symbol.Of(NonTerminalSymbol.FunctionDeclaration));
        var functionParameters = Atom(Symbol.Of(NonTerminalSymbol.FunctionParameters));
        var functionParameterDeclaration = Atom(Symbol.Of(NonTerminalSymbol.FunctionParameter));
        var functionParameterDeclarationDefVal = Atom(Symbol.Of(NonTerminalSymbol.FunctionParameterWithDefaultValue));

        // Terminal
        var semicolon = Atom(Symbol.Of(LexicalCategory.Semicolon));
        var comma = Atom(Symbol.Of(LexicalCategory.Comma));
        var colon = Atom(Symbol.Of(LexicalCategory.Colon));

        var braceOpen = Atom(Symbol.Of(LexicalCategory.BracesAndParentheses, "{"));
        var braceClose = Atom(Symbol.Of(LexicalCategory.BracesAndParentheses, "}"));
        var parOpen = Atom(Symbol.Of(LexicalCategory.BracesAndParentheses, "("));
        var parClose = Atom(Symbol.Of(LexicalCategory.BracesAndParentheses, ")"));

        var identifier = Atom(Symbol.Of(LexicalCategory.VariableIdentifiers));
        var typeIdentifier = Atom(Symbol.Of(LexicalCategory.TypeIdentifiers));
        var trueLiteral = Atom(Symbol.Of(LexicalCategory.Literals, "true"));
        var falseLiteral = Atom(Symbol.Of(LexicalCategory.Literals, "false"));
        var digitLiteral = Atom(Symbol.Of(LexicalCategory.Literals));

        var ifKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "if"));
        var elseKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "else"));
        var loopKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "loop"));
        var breakKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "break"));
        var continueKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "continue"));
        var returnKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "return"));
        var varKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "var"));
        var constKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "const"));
        var funKeyword = Atom(Symbol.Of(LexicalCategory.Keywords, "fun"));

        var scAndOperator = Atom(Symbol.Of(LexicalCategory.Operators, "&&"));
        var scOrOperator = Atom(Symbol.Of(LexicalCategory.Operators, "||"));
        var equalsOperator = Atom(Symbol.Of(LexicalCategory.Operators, "=="));
        var greaterOperator = Atom(Symbol.Of(LexicalCategory.Operators, ">"));
        var lessOperator = Atom(Symbol.Of(LexicalCategory.Operators, "<"));
        var greaterOrEqualOperator = Atom(Symbol.Of(LexicalCategory.Operators, ">="));
        var lessOrEqualOperator = Atom(Symbol.Of(LexicalCategory.Operators, "<="));
        var plusOperator = Atom(Symbol.Of(LexicalCategory.Operators, "+"));
        var minusOperator = Atom(Symbol.Of(LexicalCategory.Operators, "-"));
        var assignOperator = Atom(Symbol.Of(LexicalCategory.Operators, "="));

        // Aliases
        var aliasBlockExpression = Union(codeBlock, codeGroup, ifExpression, loopExpression, functionDeclaration);
        var aliasClosedExpression =
            Union(Concat(openExpression, semicolon), Concat(aliasBlockExpression, Optional(semicolon)));
        var aliasExpression = Union(openExpression, aliasBlockExpression);

        productions
            .Add(program, expressionSeq)

            // Statements
            .Add(expressionSeq, Concat(Star(aliasClosedExpression), Optional(openExpression)))
            .Add(codeBlock, Concat(braceOpen, expressionSeq, braceClose))
            .Add(codeGroup, Concat(
                    parOpen,
                    Union(
                        Concat(aliasExpression, semicolon),
                        Concat(aliasClosedExpression, Star(aliasClosedExpression), Union(openExpression, aliasClosedExpression))),
                    parClose))

            // Expression
            .Add(openExpression,
                Union(variableDeclaration, assignment, breakKeyword, continueKeyword, returnExpression))
            .Add(returnExpression, Concat(returnKeyword, Optional(aliasExpression)))

            .Add(openExpression, Union(
                logicalOperand, // anything but a block-expression
                Concat(
                    Star(Union(logicalOperand, aliasBlockExpression), logicalOperator),
                    Union(logicalOperand, aliasBlockExpression), logicalOperator,
                    Union(logicalOperand, aliasBlockExpression))))
            .Add(logicalOperator, Union(scAndOperator, scOrOperator))
            .Add(logicalOperand, Union(
                comparisonOperand, // anything but a block-expression
                Concat(
                    Star(Union(comparisonOperand, aliasBlockExpression), comparisonOperator),
                    Union(comparisonOperand, aliasBlockExpression), comparisonOperator,
                    Union(comparisonOperand, aliasBlockExpression))))
            .Add(comparisonOperator,
                Union(equalsOperator, greaterOperator, lessOperator, greaterOrEqualOperator, lessOrEqualOperator))
            .Add(comparisonOperand, Union(
                arithmeticOperand, // anything but a block-expression
                Concat(
                    Star(Union(arithmeticOperand, aliasBlockExpression), arithmeticOperator),
                    Union(arithmeticOperand, aliasBlockExpression), arithmeticOperator,
                    Union(arithmeticOperand, aliasBlockExpression))))
            .Add(arithmeticOperator, Union(plusOperator, minusOperator))
            .Add(arithmeticOperand, simpleExpression)

            // Simple expression
            .Add(simpleExpression, literalValue)
            .Add(literalValue, Union(trueLiteral, falseLiteral, digitLiteral))
            .Add(simpleExpression, Concat(parOpen, aliasExpression, parClose)) // (E) or ({})
            .Add(simpleExpression, Union(identifier, functionCall)) // x or f()
            .Add(functionCall, Concat(identifier, parOpen, functionArguments, parClose))
            .Add(functionArguments,
                Optional(Concat(aliasExpression, Star(comma, aliasExpression))))

            // Block expressions
            .Add(ifExpression, Concat(
                ifKeyword, ifCondition, codeBlock,
                Optional(Concat(elseKeyword, codeBlock))))
            .Add(ifCondition, Union(
                codeGroup, // (E;E;E)
                Concat(parOpen, aliasExpression, parClose)))// (E) or ({}))
            .Add(loopExpression, Concat(loopKeyword, codeBlock))

            // Assignment
            .Add(assignment,
                Concat(identifier, assignOperator, aliasExpression))

            // Declarations
            .Add(modifier, Union(varKeyword, constKeyword))
            .Add(typeSpec, Concat(colon, typeIdentifier))
            .Add(variableDeclaration,
                Concat(modifier, Union(assignment, Concat(identifier, typeSpec, Optional(Concat(assignOperator, aliasExpression))))))
            .Add(functionDeclaration,
                Concat(funKeyword, identifier, parOpen, functionParameters, parClose, Optional(typeSpec), codeBlock))
            .Add(functionParameters, Optional(Union(
                Concat(functionParameterDeclarationDefVal, Star(comma, functionParameterDeclarationDefVal)),
                Concat(functionParameterDeclaration, Star(comma, functionParameterDeclaration),
                    Optional(Concat(comma, functionParameterDeclarationDefVal, Star(comma, functionParameterDeclarationDefVal)))) // only suffix with default values
                )))
            .Add(functionParameterDeclaration, Concat(identifier, typeSpec))
            .Add(functionParameterDeclarationDefVal, Concat(identifier, typeSpec, assignOperator, literalValue));

        return new Grammar<Symbol>(program, productions);
    }

    private static Regex Optional(Regex regex) => Union(Epsilon, regex);
    private static Regex Star(params Regex[] regexRest) => Regex.Star(Concat(regexRest));

    private static List<Production> Add(this List<Production> list, Symbol lhs, Regex rhs)
    {
        list.Add(new Production(lhs, rhs));
        return list;
    }

    private static List<Production> Add(this List<Production> list, Regex lhs, Regex rhs)
    {
        var atomRegex = lhs as AtomRegex<Symbol>;
        Debug.Assert(atomRegex is not null);
        return list.Add(atomRegex.Atom, rhs);
    }
}
