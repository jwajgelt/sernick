namespace sernick.Ast.Nodes;

using Input;
using Utility;

/// <summary>
/// Represents declaration of a struct with all its fields.
/// </summary>
public sealed record StructDeclaration(Identifier Name, IReadOnlyCollection<FieldDeclaration> Fields,
    Range<ILocation> LocationRange) : Declaration(Name, LocationRange)
{
    public override IEnumerable<AstNode> Children => Name.Enumerate().Concat<AstNode>(Fields);

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitStructDeclaration(this, param);
}

/// <summary>
/// Represents declaration of an individual field in a struct.
/// </summary>
public sealed record FieldDeclaration(Identifier Name, Type Type, Range<ILocation> LocationRange) : Declaration(Name,
    LocationRange)
{
    public override IEnumerable<AstNode> Children => Name.Enumerate();

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFieldDeclaration(this, param);
}

/// <summary>
/// Represents creation of a new struct object.
/// </summary>
public sealed record StructValue(Identifier StructName, IReadOnlyCollection<StructFieldInitializer> Fields,
    Range<ILocation> LocationRange) : SimpleValue(LocationRange)
{
    public override IEnumerable<AstNode> Children => StructName.Enumerate().Concat<AstNode>(Fields);

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitStructValue(this, param);
}

/// <summary>
/// Represents value assigment to a struct field in StructValue
/// </summary>
public sealed record StructFieldInitializer(Identifier FieldName, Expression Value,
    Range<ILocation> LocationRange) : AstNode(LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode[] { FieldName, Value };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitStructFieldInitializer(this, param);
}

/// <summary>
/// Represents accessing a field of a struct object.
/// If Left is a pointer to a struct then this evaluates to <code>(*pointer).field</code>.
/// </summary>
public sealed record StructFieldAccess(Expression Left, Identifier FieldName, Range<ILocation> LocationRange) : SimpleValue(LocationRange)
{
    public override IEnumerable<AstNode> Children => new AstNode[] { Left, FieldName };

    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.StructFieldAccess(this, param);
}
