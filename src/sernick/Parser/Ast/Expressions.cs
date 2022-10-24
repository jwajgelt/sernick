namespace sernick.Parser.Ast;

/// <summary>
/// Class for code blocks (introducing new scope)
/// </summary>
public sealed record CodeBlock(Expression Inner) : Expression;

/// <summary>
/// Class representing expressions which consist of many expressions (use of ;)
/// </summary>
public sealed record ExpressionJoin(Expression First, Expression Second) : Expression;

/// <summary>
/// Class representing function calls
/// </summary>
public sealed record FunctionCall(Identifier FunctionName,
    IEnumerable<Expression> Arguments) : Expression;

/// <summary>
/// Base class for all expressions which are created through use of operators
/// </summary>
public abstract record Operator : Expression { }

/// <summary>
/// Base class for expressions which represent some type of declaration
/// </summary>
public abstract record Declaration : Expression { }

/// <summary>
/// Base class for classes representing flow control statements
/// </summary>
public abstract record FlowControlStatement : Expression { }

/// <summary>
/// Base class for classes representing "simple value" expressions
/// eg. values of variables, literals
/// </summary>
public abstract record SimpleValue : Expression { }
