namespace sernick.Grammar.Syntax;
using LexicalCategory = Lexicon.LexicalGrammarCategory;

public abstract record Symbol;

public sealed record Terminal(LexicalCategory Category, string Text) : Symbol
{
    public bool Equals(Terminal? other) => Category switch
    {
        LexicalCategory.BracesAndParentheses or LexicalCategory.Operators or LexicalCategory.Keywords => (Category, Text) == (other?.Category, other?.Text),
        _ => (Category == other?.Category)
    };

    public override int GetHashCode() => Category switch
    {
        LexicalCategory.BracesAndParentheses or LexicalCategory.Operators or LexicalCategory.Keywords => HashCode.Combine(Category, Text),
        _ => Category.GetHashCode()
    };
}

public sealed record NonTerminal(NonTerminalSymbol Inner) : Symbol;

public enum NonTerminalSymbol
{
    Start,
    Program,
    Expression,
    JoinableExpression,
    CodeBlock,
    IfStatement,
    LoopStatement,
    VariableDeclaration,
    ConstDeclaration,
    FunctionDefinition,
    FunctionCall,
    Assignment,
    ElseBlock,
    // lower number == lower priority
    BinaryOperatorPriority1,
    X1,
    BinaryOperatorPriority2,
    X2,
    BinaryOperatorPriority3,
    X3,
}
