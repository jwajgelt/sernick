namespace sernick.Ast;

using System.Drawing;

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
