namespace sernick.Ast.Nodes;

using Conversion;
using Grammar.Lexicon;
using Grammar.Syntax;
using Input;
using Parser.ParseTree;
using Utility;

/// <summary>
/// Base class for all types of nodes that can appear in AST (Abstract Syntax Tree)
/// </summary>
public abstract record AstNode(Range<ILocation> LocationRange)
{
    public virtual IEnumerable<AstNode> Children => Enumerable.Empty<AstNode>();

    /// <summary>
    /// Constructs the Abstract Syntax Tree from the given parse tree
    /// <param name="parseTree">Parse tree to generate the AST from</param>
    /// </summary>
    public static AstNode From(IParseTree<Symbol> parseTree)
    {
        return parseTree switch
        {
            { Symbol: Terminal { Category: LexicalGrammarCategory.TypeIdentifiers } }
                => parseTree.ToIdentifier(),
            _ => parseTree.ToExpression()
        };
    }

    public abstract TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param);
}

/// <summary>
/// Class representing identifiers
/// </summary>
public sealed record Identifier(string Name, Range<ILocation> LocationRange) : AstNode(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitIdentifier(this, param);
}

/// <summary>
/// Base class for all types of expressions
/// </summary>
public abstract record Expression(Range<ILocation> LocationRange) : AstNode(LocationRange);
