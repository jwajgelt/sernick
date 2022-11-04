namespace sernick.Grammar.Syntax;
using sernick.Grammar.Lexicon;

public abstract record Symbol;

public record Terminal(LexicalGrammarCategoryType symbol) : Symbol;

public record NonTerminal(NonTerminalSymbol symbol) : Symbol;

public enum NonTerminalSymbol 
{
    Program,
    Expression,
    CodeBlock,
    IfStatement,
    WhileStatement,
    
}