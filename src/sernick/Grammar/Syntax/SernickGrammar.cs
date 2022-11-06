namespace sernick.Grammar.Syntax;
using sernick.Common.Regex;
using sernick.Input;
using sernick.Tokenizer;
using Cat = Lexicon.LexicalGrammarCategoryType;
using Regex = Common.Regex.Regex<Symbol>;

internal sealed record PlaceholderLocation : ILocation { };

public static class SernickGrammarProvider
{
    public static Grammar<Symbol> createSernickGrammar()
    {
        var productions = new List<Production<Symbol>>();
        var I = new PlaceholderLocation();

        Symbol program = new NonTerminal(NonTerminalSymbol.Program);
        Symbol expression = new NonTerminal(NonTerminalSymbol.Expression);
        Symbol codeBlock = new NonTerminal(NonTerminalSymbol.CodeBlock);
        Symbol ifStatement = new NonTerminal(NonTerminalSymbol.IfStatement);
        Symbol loopStatement = new NonTerminal(NonTerminalSymbol.LoopStatement);
        Symbol varDeclaration = new NonTerminal(NonTerminalSymbol.VariableDeclaration);

        Symbol semicolon = new Terminal(new Token<Cat>(Cat.Semicolon, ";", I, I));

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
