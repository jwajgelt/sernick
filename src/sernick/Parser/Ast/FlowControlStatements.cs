namespace sernick.Parser.Ast;

public sealed record ContinueStatement : FlowControlStatement { }

public sealed record ReturnStatement(Expression ReturnValue) : FlowControlStatement;

public sealed record BreakStatement : FlowControlStatement { }

public sealed record IfStatement(Expression Condition,
    CodeBlock IfBlock, CodeBlock? ElseBlock) : FlowControlStatement;

public sealed record LoopStatement(CodeBlock Inner) : FlowControlStatement;
