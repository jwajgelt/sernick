using sernick.Parser.Ast;

public sealed record ContinueStatement : FlowControlStatement { }

public sealed record ReturnStatement(Expression returnValue) : FlowControlStatement;

public sealed record BreakStatement : FlowControlStatement { }

public sealed record IfStatement(Expression testExpression,
    CodeBlock ifBlock, CodeBlock? elseBlock) : FlowControlStatement;

public sealed record LoopStatement(CodeBlock inner) : FlowControlStatement;
