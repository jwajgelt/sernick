namespace sernick.Grammar.Syntax;
using sernick.Tokenizer;
using sernick.Grammar.Lexicon;
using sernick.Input;
using sernick.Common.Regex;

using Regex = Common.Regex.Regex<Symbol>;
using Cat = Lexicon.LexicalGrammarCategoryType;

sealed record PlaceholderLocation : ILocation {};

public static class SernickGrammarProvider
{
    public static Grammar<Symbol> createSernickGrammar()
    {
        var productions = new List<Production<Symbol>>();
        PlaceholderLocation I = new PlaceholderLocation();

        Symbol program = new NonTerminal(NonTerminalSymbol.Program);
        Symbol expression = new NonTerminal(NonTerminalSymbol.Expression);
        Symbol codeBlock = new NonTerminal(NonTerminalSymbol.CodeBlock);
        Symbol ifStatement = new NonTerminal(NonTerminalSymbol.IfStatement);
        Symbol loopStatement = new NonTerminal(NonTerminalSymbol.LoopStatement);
        Symbol variableDeclaration = new NonTerminal(NonTerminalSymbol.VariableDeclaration);

        Symbol semicolon = new Terminal(new Token<Cat>(Cat.Semicolon, ";", I, I));

        var reg_expression = Regex<Symbol>.Atom(expression);
        var reg_semicolon = Regex<Symbol>.Atom(semicolon);

        productions.Add(new Production<Symbol>(
            program,
            reg_expression
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat((reg_expression, reg_semicolon, reg_expression))
        ));

        productions.Add(new Production<Symbol>(
            expression,
            Regex.Concat((reg_expression, reg_semicolon, reg_expression))
        ));

        return new Grammar<Symbol>(
            new NonTerminal(NonTerminalSymbol.Program),
            productions
        );
    }
}
