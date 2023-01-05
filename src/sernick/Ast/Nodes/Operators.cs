namespace sernick.Ast.Nodes;

using Input;
using Utility;

public sealed record Infix
    (Expression Left, Expression Right, Infix.Op Operator, Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public Infix(Expression left, Expression right, Infix.Op op) : this(left, right, op,
        (left.LocationRange.Start, right.LocationRange.End))
    { }

    public enum Op
    {
        Plus, Minus,
        Less, Greater,
        LessOrEquals, GreaterOrEquals,
        Equals, NotEqual,
        ScAnd, ScOr
    }

    public override IEnumerable<AstNode> Children => new[] { Left, Right };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitInfix(this, param);
}

public sealed record Assignment(Identifier Left, Expression Right, Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode[] { Left, Right };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitAssignment(this, param);
}
