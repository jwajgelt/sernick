#pragma warning disable IDE0052

namespace sernick.Parser.Ast;

/// <summary>
/// Base class for all types of nodes that can appear in AST (Abstract Syntax Tree)
/// </summary>
public abstract record AstNode { }

/// <summary>
/// Class representing identifiers
/// </summary>
public record Identifier(string Name) : AstNode;

/// <summary>
/// Base class for all types of expressions
/// </summary>
public abstract record Expression : AstNode { }

/// <summary>
/// Base class for types declared eg. in variable declarations
/// </summary>
public abstract record DeclaredType : AstNode { }
