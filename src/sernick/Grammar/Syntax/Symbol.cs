namespace sernick.Grammar.Syntax;
using sernick.Grammar.Lexicon;
using sernick.Tokenizer;

public abstract record Symbol;

public record Terminal(Token<LexicalGrammarCategoryType> symbol) : Symbol;

public record NonTerminal(NonTerminalSymbol symbol) : Symbol;

public enum NonTerminalSymbol
{
    Program,
    Expression,
    CodeBlock,
    IfStatement,
    LoopStatement,
    VariableDeclaration,
    ConstDeclaration,
    FunctionDefinition,
    FuctionCall,
    Assignment,
    ExpSummand,
    ExpFactor
}
