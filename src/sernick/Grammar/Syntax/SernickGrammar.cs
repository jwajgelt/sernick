namespace sernick.Grammar.Syntax;

public static class SernickGrammarProvider 
{
    public static Grammar<Symbol> createSernickGrammar() {
        return new Grammar<Symbol>(
                new NonTerminal(NonTerminalSymbol.Program),
                new List<Production<Symbol>>()
            );
    }
}