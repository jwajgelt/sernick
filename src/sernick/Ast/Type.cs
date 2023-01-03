namespace sernick.Ast;
using sernick.Ast.Nodes;

/// <summary>
/// Base class for Sernick types
/// </summary>
public abstract record Type { }

public sealed record BoolType : Type
{
    public override string ToString() => "Bool";
}

public sealed record IntType : Type
{
    public override string ToString() => "Int";
}

public sealed record UnitType : Type
{
    public override string ToString() => "Unit";
}

public sealed record PointerType(Type Type) : Type
{
    public override string ToString() => $"*{Type}";
}

public sealed record StructType(StructIdentifier Struct) : Type;
