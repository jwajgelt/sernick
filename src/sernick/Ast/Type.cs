namespace sernick.Ast;

/// <summary>
/// Base class for Sernick types
/// </summary>
public abstract class Type { }

public class BoolType : Type
{
    public override string ToString() => "Bool";
}


public class IntType : Type
{
    public override string ToString() => "Int";
}

public class UnitType : Type
{
    public override string ToString() => "Unit";
}
