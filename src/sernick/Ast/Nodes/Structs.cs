namespace sernick.Ast.Nodes;

using Input;
using Utility;

public sealed record StructIdentifier(string Name, Range<ILocation> LocationRange) : Identifier(Name, LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitStructIdentifier(this, param);
}

/// <summary>
/// Represents declaration of a struct with all its fields.
/// </summary>
public sealed record StructDeclaration(StructIdentifier StructName, IReadOnlyCollection<FieldDeclaration> Fields,
    Range<ILocation> LocationRange) : Declaration(StructName, LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitStructDeclaration(this, param);
}

/// <summary>
/// Represents declaration of an individual field in a struct.
/// </summary>
public sealed record FieldDeclaration(Identifier Name, Type Type, Range<ILocation> LocationRange) : Declaration(Name,
    LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFieldDeclaration(this, param);
}

/// <summary>
/// Represents creation of a new struct object.
/// </summary>
public sealed record StructValue(StructIdentifier StructName, IReadOnlyDictionary<string, StructFieldValue> Fields,
    Range<ILocation> LocationRange) : SimpleValue(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitStructValue(this, param);
}

/// <summary>
/// Represents value assigment to a struct field in StructValue
/// </summary>
public sealed record StructFieldValue(Identifier FieldName, Expression Value,
    Range<ILocation> LocationRange) : AstNode(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitStructFieldValue(this, param);
}

/// <summary>
/// Represents accessing a field of a struct object.
/// If Left is a pointer to a struct then this evaluates to <code>(*pointer).field</code>.
/// </summary>
public sealed record FieldAccess(Expression Left, Identifier FieldName, Range<ILocation> LocationRange): Expression(LocationRange)
{
    public override TResult Accept<TResult, TParam>(AstVisitor<TResult, TParam> visitor, TParam param) =>
        visitor.VisitFieldAccess(this, param);
}
