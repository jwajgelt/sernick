namespace sernick.Parser.Ast;

public sealed record ConstDeclaration(Identifier Name,
    DeclaredType? Type,
    Expression? InitValue) : Declaration;

public sealed record VariableDeclaration(Identifier Name,
    DeclaredType? Type,
    Expression? InitValue) : Declaration;

public sealed record FunctionParameterDeclaration(Identifier Name,
    DeclaredType Type,
    LiteralValue? DefaultValue) : Declaration;

public record FunctionDefinition(Identifier Name,
    IEnumerable<FunctionParameterDeclaration> Parameters,
    DeclaredType ReturnType,
    CodeBlock Body) : Declaration;
