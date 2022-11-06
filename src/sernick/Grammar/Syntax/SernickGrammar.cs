namespace sernick.Grammar.Syntax;
using sernick.Common.Regex;

public static class SernickGrammarProvider
{
    public static Grammar<Symbol> createSernickGrammar()
    {
        var productions = new List<Production<Symbol>>();

        Symbol program = new NonTerminal(NonTerminalSymbol.Program);
        Symbol expression = new NonTerminal(NonTerminalSymbol.Expression);

        productions.Add(new Production<Symbol>(
            program,
            Regex<Symbol>.Atom(expression)
        ));

        return new Grammar<Symbol>(
            new NonTerminal(NonTerminalSymbol.Program),
            productions
        );
    }
}
