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
        _ = new NonTerminal(NonTerminalSymbol.VariableDeclaration);

        // Terminal
        var semicolon = new Terminal(LexicalCategory.Semicolon, ";");
        var comma = new Terminal(LexicalCategory.Comma, ","); 
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

        // Atomic regular expressions representing symbols

        // For non-terminal
        var regExpression = Regex.Atom(expression);
        var regCodeBlock = Regex.Atom(codeBlock);
        var regElseBlock = Regex.Atom(elseBlock);

        // For terminal
        var regSemicolon = Regex.Atom(semicolon);
        var regComma = Regex.Atom(comma);
        var regBracesOpen = Regex.Atom(bracesOpen);
        var regBracesClose = Regex.Atom(bracesClose);
        var regParenthesesOpen = Regex.Atom(parenthesesOpen);
        var regParenthesesClose = Regex.Atom(parenthesesClose);
        var regLoopKeyword = Regex.Atom(loopKeyword);
        var regIfKeyword = Regex.Atom(ifKeyword);
        var regElseKeyword = Regex.Atom(elseKeyword);
        var regIdentifier = Regex.Atom(identifier);

        productions.Add(new Production<Symbol>(
            program,
            regExpression
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { regExpression, regSemicolon, regExpression })
        ));

        productions.Add(new Production<Symbol>(
            codeBlock,
            Regex.Concat(new Regex[] { regBracesOpen, regExpression, regBracesClose })
        ));

        productions.Add(new Production<Symbol>(
            loopStatement,
            Regex.Concat(new Regex[] { regLoopKeyword, regCodeBlock })
        ));

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

        // Prepering regexes for function call production
        Regex argStarred = Regex.Star(Regex.Concat(new Regex[] { regExpression, regComma }));
        Regex regArgList = Regex.Union(Regex.Concat(argStarred, regExpression), Regex.Epsilon);

        productions.Add(new Production<Symbol>(
            functionCall,
            Regex.Concat(new Regex[] { regIdentifier, regParenthesesOpen, regArgList, regParenthesesClose })
        ));

        return new Grammar<Symbol>(
            new NonTerminal(NonTerminalSymbol.Program),
            productions
        );
    }
}
