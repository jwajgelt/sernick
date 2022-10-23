#pragma warning disable IDE0052

namespace sernick.Parser.Ast;

/// <summary>
/// Base class for all types of nodes that can appear in AST (Abstract Syntax Tree)
/// </summary>
public abstract record AstNode { }

public record Identifier(string Name) : AstNode;

/// <summary>
/// Base class for all types of expressions
/// </summary>
public abstract record Expression : AstNode { }

/// <summary>
/// Class for code blocks (introducing new scope)
/// </summary>
public sealed record CodeBlock(Expression inner) : Expression;

/// <summary>
/// Class representing expressions which consist of many expressions (use of ;)
/// </summary>
public sealed record ExpressionJoin(IEnumerable<Expression> inner) : Expression;

/// <summary>
/// Class representing function calls
/// </summary>
public sealed record FunctionCall(Identifier functionName,
    IEnumerable<Expression> argList) : Expression;

/// <summary>
/// Base class for all expressions which are created through use of operators
/// </summary>
public abstract record Operator : Expression { }

public sealed record PlusOperator(Expression left, Expression right) : Operator;

public sealed record MinusOperator(Expression left, Expression right) : Operator;

public sealed record EqualsOperator(Expression left, Expression right) : Operator;

public sealed record AssignOperator(Identifier left, Expression right) : Operator;

/// <summary>
/// Base class for types declared eg. in variable declarations
/// </summary>
public abstract record DeclaredType { }

public sealed record BoolType : DeclaredType { }

public sealed record IntType : DeclaredType { }

public sealed record UnitType : DeclaredType { }

public sealed record NoType : DeclaredType { }

/// <summary>
/// Base class for expressions which represent some type of declaration
/// </summary>
public abstract record Declaration : Expression { }

public sealed record ConstDeclaration(Identifier name,
    DeclaredType DeclaredType,
    Expression initValue) : Declaration;

public sealed record VariableDeclaration(Identifier name,
    DeclaredType declaredType,
    Expression initValue) : Declaration;

public record FunctionDefinition(Identifier name,
    IEnumerable<ConstDeclaration> argsDeclaration,
    DeclaredType returnType,
    CodeBlock functionBody) : Declaration;

/// <summary>
/// Base class for classes representing flow control statements
/// </summary>
public abstract record FlowControlStatement : Expression { }

public sealed record ContinueStatement : FlowControlStatement { }

public sealed record ReturnStatement(Expression returnValue) : FlowControlStatement;

public sealed record BreakStatement : FlowControlStatement { }

public sealed record IfStatement(Expression testExpression,
    CodeBlock ifBlock, CodeBlock elseBlock) : FlowControlStatement;

public sealed record LoopStatement(CodeBlock inner) : FlowControlStatement;

/// <summary>
/// Base class for classes representing "simple value" expressions
/// eg. values of variables, literals
/// </summary>
public abstract record SimpleValue : Expression { }

public sealed record ConstValue(Identifier identifier) : SimpleValue;

public sealed record VariableValue(Identifier identifier) : SimpleValue;

public abstract record LiteralValue : SimpleValue { }

public sealed record BoolLiteralValue(bool value) : LiteralValue;

public sealed record IntLiteralValue(bool value) : LiteralValue;

public sealed record NoValue : SimpleValue { }
