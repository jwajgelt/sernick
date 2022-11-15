namespace sernick.Ast.Nodes;

using Input;
using Utility;

public sealed record ContinueStatement(Range<ILocation> LocationRange) : FlowControlStatement(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitContinueStatement(this, param);
}

public sealed record ReturnStatement(Expression? ReturnValue, Range<ILocation> LocationRange) : FlowControlStatement(LocationRange)
{
    public override IEnumerable<AstNode> Children => ReturnValue != null ? new[] { ReturnValue } : Enumerable.Empty<AstNode>();

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitReturnStatement(this, param);
}

public sealed record BreakStatement(Range<ILocation> LocationRange) : FlowControlStatement(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitBreakStatement(this, param);
}

public sealed record IfStatement(Expression Condition, CodeBlock IfBlock, CodeBlock? ElseBlock, Range<ILocation> LocationRange) : FlowControlStatement(LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode?[] { Condition, IfBlock, ElseBlock }.OfType<AstNode>();

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitIfStatement(this, param);
}

public sealed record LoopStatement(CodeBlock Inner, Range<ILocation> LocationRange) : FlowControlStatement(LocationRange)
{
    public override IEnumerable<AstNode> Children => new[] { Inner };
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitLoopStatement(this, param);
}
