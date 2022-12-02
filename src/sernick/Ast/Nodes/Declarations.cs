namespace sernick.Ast.Nodes;

using Compiler.Function;
using Input;
using Utility;

public sealed record VariableDeclaration(Identifier Name,
    Type? Type,
    Expression? InitValue,
    bool IsConst,
    Range<ILocation> LocationRange) : Declaration(Name, LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode?[] { Name, InitValue }.OfType<AstNode>();

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitVariableDeclaration(this, param);
}

public sealed record FunctionParameterDeclaration(Identifier Name,
    Type Type,
    LiteralValue? DefaultValue,
    Range<ILocation> LocationRange) : Declaration(Name, LocationRange), IFunctionParam
{
    public override IEnumerable<AstNode> Children => new AstNode?[] { Name, DefaultValue }.OfType<AstNode>();

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFunctionParameterDeclaration(this, param);

    public Object? TryGetDefaultValue()
    {
        return this.DefaultValue switch
        {
            null => null,
            IntLiteralValue intValue => intValue.Value,
            BoolLiteralValue boolValue => boolValue.Value,
            _ => throw new Exception("Default value of unsupported type."),
        };
    }
}

public record FunctionDefinition(Identifier Name,
    IEnumerable<FunctionParameterDeclaration> Parameters,
    Type ReturnType,
    CodeBlock Body,
    Range<ILocation> LocationRange) : Declaration(Name, LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode[] { Name }.Concat(Parameters).Append(Body);

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFunctionDefinition(this, param);

    public override string ToString()
    {
        return Name.Name;
    }
}
