namespace sernick.Grammar.Syntax;

using Regex = Common.Regex.Regex<Symbol>;

public static class SernickGrammar
{
    public static Grammar<Symbol> Create()
    {
        var productions = new List<Production<Symbol>>();

        var program = new NonTerminal(NonTerminalSymbol.Program);
        var expression = new NonTerminal(NonTerminalSymbol.Expression);

        productions.Add(new Production<Symbol>(
            program,
            Regex.Atom(expression)
        ));

        return new Grammar<Symbol>(
            new NonTerminal(NonTerminalSymbol.Program),
            productions
        );
    }
}
