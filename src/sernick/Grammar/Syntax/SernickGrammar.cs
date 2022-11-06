namespace sernick.Grammar.Syntax;
using sernick.Common.Regex;
using sernick.Input;
using sernick.Tokenizer;
using LexicalCategory = Lexicon.LexicalGrammarCategoryType;
using Regex = Common.Regex.Regex<Symbol>;

public static class SernickGrammar
{
    public static Grammar<Symbol> Create()
    {
        var productions = new List<Production<Symbol>>();

        var program = new NonTerminal(NonTerminalSymbol.Program);
        var expression = new NonTerminal(NonTerminalSymbol.Expression);
        var codeBlock = new NonTerminal(NonTerminalSymbol.CodeBlock);
        var ifStatement = new NonTerminal(NonTerminalSymbol.IfStatement);
        var loopStatement = new NonTerminal(NonTerminalSymbol.LoopStatement);
        var varDeclaration = new NonTerminal(NonTerminalSymbol.VariableDeclaration);

        var semicolon = new Terminal(LexicalCategory.Semicolon, ";");

        var reg_expression = Regex<Symbol>.Atom(expression);
        var reg_semicolon = Regex<Symbol>.Atom(semicolon);

        productions.Add(new Production<Symbol>(
            program,
            reg_expression
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat(new Regex[] { reg_expression, reg_semicolon, reg_expression })
        ));

        return new Grammar<Symbol>(
            new NonTerminal(NonTerminalSymbol.Program),
            productions
        );
    }
}
