namespace sernick.Grammar.Syntax;
using sernick.Tokenizer;

using Cat = Lexicon.LexicalGrammarCategoryType;

public abstract record Symbol;

public sealed record Terminal(Token<Cat> Inner) : Symbol
{
    public bool Equals(Terminal? other) => Inner.Category switch
    {
        Cat.BracesAndParentheses or Cat.Operators or Cat.Keywords => (Inner.Category, Inner.Text) == (other?.Inner.Category, other?.Inner.Text),
        _ => (Inner.Category == other?.Inner.Category)
    };

    public override int GetHashCode()
    {
        int hash_code;
        if (Inner.Category is Cat.BracesAndParentheses or Cat.Operators or Cat.Keywords)
        {
            hash_code = HashCode.Combine(Inner.Category, Inner.Text);
        }
        else
        {
            hash_code = Inner.Category.GetHashCode();
        }

        return hash_code;
    }
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
