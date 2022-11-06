namespace sernick.Grammar.Syntax;
using sernick.Tokenizer;

using Cat = Lexicon.LexicalGrammarCategoryType;

public abstract record Symbol;

public sealed record Terminal(Token<Cat> Inner) : Symbol
{
    public bool Equals(Terminal? other)
    {
        bool comp_res;
        if (Inner.Category == Cat.BracesAndParentheses ||
            Inner.Category == Cat.Operators ||
            Inner.Category == Cat.Keywords)
        {
            comp_res = (other != null) &&
                    (Inner.Category == other.Inner.Category) &&
                    (Inner.Text == other.Inner.Text);
        }
        else
        {
            comp_res = (other != null) && (Inner.Category == other.Inner.Category);
        }

        return comp_res;
    }

    public override int GetHashCode()
    {
        int hash_code;
        if (Inner.Category == Cat.BracesAndParentheses ||
            Inner.Category == Cat.Operators ||
            Inner.Category == Cat.Keywords)
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
