namespace sernick.Ast.Nodes;

using Input;
using Utility;

public sealed record Infix(Expression Left, Expression Right, Infix.Op Operator, Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public enum Op
    {
        Plus, Minus,
        Less, Greater,
        LessOrEquals, GreaterOrEquals,
        Equals,
        ScAnd, ScOr
    }

    public override IEnumerable<AstNode> Children => new[] { Left, Right };
    public Expression LeftSide = Left;
    public Expression RightSide = Right;

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitInfix(this, param);
}

public sealed record Assignment(Identifier Left, Expression Right, Range<ILocation> LocationRange) : Expression(LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode[] { Left, Right };
    public Identifier LeftSide = Left;
    public Expression RightSide = Right;

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitAssignment(this, param);
}
