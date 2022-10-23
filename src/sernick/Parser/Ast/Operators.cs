namespace sernick.Parser.Ast;

public sealed record PlusOperator(Expression left, Expression right) : Operator;

public sealed record MinusOperator(Expression left, Expression right) : Operator;

public sealed record EqualsOperator(Expression left, Expression right) : Operator;

public sealed record AssignOperator(Identifier left, Expression right) : Operator;
