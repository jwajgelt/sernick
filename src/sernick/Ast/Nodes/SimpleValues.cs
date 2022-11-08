namespace sernick.Ast.Nodes;

public sealed record VariableValue(Identifier Identifier) : SimpleValue;

public abstract record LiteralValue : SimpleValue { }

public sealed record BoolLiteralValue(bool Value) : LiteralValue;

public sealed record IntLiteralValue(int Value) : LiteralValue;
