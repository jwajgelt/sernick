namespace sernick.Ast.Nodes;

using Input;
using Utility;

public sealed record VariableValue(Identifier Identifier, Range<ILocation> LocationRange) : SimpleValue(LocationRange)
{
    public override IEnumerable<AstNode> Children => new[] { Identifier };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitVariableValue(this, param);
}

public abstract record LiteralValue(Range<ILocation> LocationRange) : SimpleValue(LocationRange);

public sealed record BoolLiteralValue(bool Value, Range<ILocation> LocationRange) : LiteralValue(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitBoolLiteralValue(this, param);
}
public sealed record IntLiteralValue(int Value, Range<ILocation> LocationRange) : LiteralValue(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitIntLiteralValue(this, param);
}
