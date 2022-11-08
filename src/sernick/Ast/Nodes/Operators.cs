namespace sernick.Ast.Nodes;

public sealed record PlusOperator(Expression Left, Expression Right) : Operator;

public sealed record MinusOperator(Expression Left, Expression Right) : Operator;

public sealed record EqualsOperator(Expression Left, Expression Right) : Operator;

public sealed record AssignOperator(Identifier Left, Expression Right) : Operator;
