namespace sernick.Ast.Nodes;

public sealed record VariableValue(Identifier Identifier) : SimpleValue
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitVariableValue(this, param);
}

public abstract record LiteralValue : SimpleValue;

public sealed record BoolLiteralValue(bool Value) : LiteralValue
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitBoolLiteralValue(this, param);
}

public sealed record IntLiteralValue(int Value) : LiteralValue
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitIntLiteralValue(this, param);
}
