namespace sernick.Ast.Nodes;

public sealed record ContinueStatement : FlowControlStatement
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitContinueStatement(this, param);
}

public sealed record ReturnStatement(Expression ReturnValue) : FlowControlStatement
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitReturnStatement(this, param);
}

public sealed record BreakStatement : FlowControlStatement
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitBreakStatement(this, param);
}

public sealed record IfStatement(Expression Condition, CodeBlock IfBlock, CodeBlock? ElseBlock) : FlowControlStatement
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitIfStatement(this, param);
}

public sealed record LoopStatement(CodeBlock Inner) : FlowControlStatement
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitLoopStatement(this, param);
}
