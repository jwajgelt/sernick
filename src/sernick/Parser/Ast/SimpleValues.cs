using sernick.Parser.Ast;

public sealed record VariableValue(Identifier identifier) : SimpleValue;

public sealed record ConstValue(Identifier identifier) : SimpleValue;

public abstract record LiteralValue : SimpleValue { }

public sealed record BoolLiteralValue(bool value) : LiteralValue;

public sealed record IntLiteralValue(bool value) : LiteralValue;
