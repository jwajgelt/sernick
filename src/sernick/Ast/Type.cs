namespace sernick.Ast;
using Nodes;

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

public sealed record NullPointerType(Type Type) : Type
{
    public override string ToString() => "NullPointer";
}

public sealed record StructType(Identifier Struct) : Type
{
    public override string ToString() => $"{Struct.Name}";

    public IReadOnlyDictionary<Identifier, Type>? fieldTypes;
}

/// <summary>
/// Artificial type, which should not be used in a real programs
/// But is convenient in type checking, when we do not care about
/// what an expression returns. With "Any", we can specify it explicitly
/// rather than doing some implicit checks on null/undefined/etc.
/// </summary>
public sealed record AnyType : Type
{
    public override string ToString() => "Any";
}
