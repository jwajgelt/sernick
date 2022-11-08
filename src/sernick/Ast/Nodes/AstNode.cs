namespace sernick.Ast.Nodes;

using Grammar.Syntax;
using Parser.ParseTree;

/// <summary>
/// Base class for all types of nodes that can appear in AST (Abstract Syntax Tree)
/// </summary>
public abstract record AstNode
{

    /// <summary>
    /// Constructs the Abstract Syntax Tree from the given parse tree
    /// <param name="parseTree">Parse tree to generate the AST from</param>
    /// </summary>
    public static AstNode From(IParseTree<Symbol> parseTree)
    {
        throw new NotImplementedException();
    }

    public abstract TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param);
}

/// <summary>
/// Class representing identifiers
/// </summary>
public sealed record Identifier(string Name) : AstNode
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitIdentifier(this, param);
}

/// <summary>
/// Base class for all types of expressions
/// </summary>
public abstract record Expression : AstNode;
