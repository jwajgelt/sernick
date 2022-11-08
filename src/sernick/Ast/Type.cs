namespace sernick.Ast;

/// <summary>
/// Base class for Sernick types
/// </summary>
public abstract record Type { }

public sealed record BoolType : Type { }

public sealed record IntType : Type { }

public sealed record UnitType : Type { }
