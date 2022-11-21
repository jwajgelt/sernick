namespace sernick.Ast;

using System.Drawing;

/// <summary>
/// Base class for Sernick types
/// </summary>
public abstract record Type { }

public sealed record BoolType : Type, IComparable<Type>
{
    public override string ToString() => "Bool";
    public int CompareTo(Type? other)
    {
        return this.ToString().CompareTo(other?.ToString());
    }
}


public sealed record IntType : Type
{
    public override string ToString() => "Int";
}

public sealed record UnitType : Type, IComparable<Type>
{
    public override string ToString() => "Unit";
    public int CompareTo(Type? other)
    {
        return this.ToString().CompareTo(other?.ToString());
    }
}
