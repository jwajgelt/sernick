namespace sernick.Ast.Nodes;

public sealed record Infix(Expression Left, Expression Right, Infix.Op Operator) : Expression
{
    public enum Op
    {
        Plus, Minus,
        Less, Greater,
        LessOrEquals, GreaterOrEquals,
        Equals,
        ScAnd, ScOr
    }

}

public sealed record Assignment(Identifier Left, Expression Right) : Expression
{
}
