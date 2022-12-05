namespace sernick.Ast.Nodes;

using Compiler.Function;
using Input;
using Utility;

/// <summary>
/// Class for code blocks (introducing new scope)
/// </summary>
public sealed record CodeBlock(Expression Inner, Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public override IEnumerable<AstNode> Children => new[] { Inner };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitCodeBlock(this, param);
}

/// <summary>
/// Class representing expressions which consist of many expressions (use of ;)
/// </summary>
public sealed record ExpressionJoin
    (Expression First, Expression Second, Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public ExpressionJoin(Expression first, Expression second) : this(first, second,
        (first.LocationRange.Start, second.LocationRange.End))
    { }

    public override IEnumerable<AstNode> Children => new[] { First, Second };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitExpressionJoin(this, param);
}

/// <summary>
/// Class representing function calls
/// </summary>
public sealed record FunctionCall(Identifier FunctionName, IReadOnlyCollection<Expression> Arguments,
    Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode[] { FunctionName }.Concat(Arguments);

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFunctionCall(this, param);
}

/// <summary>
/// Base class for expressions which represent some type of declaration
/// </summary>
public abstract record Declaration(Identifier Name, Range<ILocation> LocationRange) : Expression(LocationRange),
    IFunctionVariable;

/// <summary>
/// Base class for classes representing flow control statements
/// </summary>
public abstract record FlowControlStatement(Range<ILocation> LocationRange) : Expression(LocationRange);

/// <summary>
/// Base class for classes representing "simple value" expressions
/// eg. values of variables, literals
/// </summary>
public abstract record SimpleValue(Range<ILocation> LocationRange) : Expression(LocationRange);

/// <summary>
/// Class representing expression which don't do anything, like: '{}'
/// </summary>
public sealed record EmptyExpression(Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param)
        => visitor.VisitEmptyExpression(this, param);
}
