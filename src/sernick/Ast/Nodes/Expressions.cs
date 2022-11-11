namespace sernick.Ast.Nodes;

/// <summary>
/// Class for code blocks (introducing new scope)
/// </summary>
public sealed record CodeBlock(Expression Inner) : Expression
{
    public override IEnumerable<AstNode> Children => new[] { Inner };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitCodeBlock(this, param);
}

/// <summary>
/// Class representing expressions which consist of many expressions (use of ;)
/// </summary>
public sealed record ExpressionJoin(Expression First, Expression Second) : Expression
{
    public override IEnumerable<AstNode> Children => new[] { First, Second };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitExpressionJoin(this, param);
}

/// <summary>
/// Class representing function calls
/// </summary>
public sealed record FunctionCall(Identifier FunctionName, IEnumerable<Expression> Arguments) : Expression
{
    public override IEnumerable<AstNode> Children => new AstNode[] { FunctionName }.Concat(Arguments);

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFunctionCall(this, param);
}

/// <summary>
/// Base class for expressions which represent some type of declaration
/// </summary>
public abstract record Declaration(Identifier Name) : Expression { }

/// <summary>
/// Base class for classes representing flow control statements
/// </summary>
public abstract record FlowControlStatement : Expression { }

/// <summary>
/// Base class for classes representing "simple value" expressions
/// eg. values of variables, literals
/// </summary>
public abstract record SimpleValue : Expression { }
