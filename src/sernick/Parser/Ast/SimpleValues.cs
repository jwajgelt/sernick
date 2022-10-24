namespace sernick.Parser.Ast;

public sealed record VariableValue(Identifier Identifier) : SimpleValue;

public sealed record ConstValue(Identifier Identifier) : SimpleValue;

public abstract record LiteralValue : SimpleValue { }

public sealed record BoolLiteralValue(bool Value) : LiteralValue;

public sealed record IntLiteralValue(int Value) : LiteralValue;
