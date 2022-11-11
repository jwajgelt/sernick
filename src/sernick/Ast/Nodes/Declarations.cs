namespace sernick.Ast.Nodes;

public sealed record VariableDeclaration(Identifier Name,
    Type? Type,
    Expression? InitValue,
    bool IsConst) : Declaration(Name)
{
    public override IEnumerable<AstNode> Children => new AstNode?[] { Name, InitValue }.OfType<AstNode>();

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitVariableDeclaration(this, param);
}

public sealed record FunctionParameterDeclaration(Identifier Name,
    Type Type,
    LiteralValue? DefaultValue) : Declaration(Name)
{
    public override IEnumerable<AstNode> Children => new AstNode?[] { Name, DefaultValue }.OfType<AstNode>();

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFunctionParameterDeclaration(this, param);
}

public record FunctionDefinition(Identifier Name,
    IEnumerable<FunctionParameterDeclaration> Parameters,
    Type ReturnType,
    CodeBlock Body) : Declaration(Name)
{
    public override IEnumerable<AstNode> Children => new AstNode[] { Name }.Concat(Parameters).Append(Body);

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFunctionDefinition(this, param);
}
