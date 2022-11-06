namespace sernick.Grammar.Syntax;
using sernick.Grammar.Lexicon;
using sernick.Tokenizer;

public abstract record Symbol;

public sealed record Terminal(Token<LexicalGrammarCategoryType> Inner) : Symbol, IEquatable<Terminal>
{
    public bool Equals(Terminal? other) =>
        (other != null) &&
        (Inner.Category == other.Inner.Category) &&
        (Inner.Text == other.Inner.Text);

    public override int GetHashCode() =>
        HashCode.Combine(Inner.Category, Inner.Text);
}

public sealed record NonTerminal(NonTerminalSymbol Inner) : Symbol;

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
