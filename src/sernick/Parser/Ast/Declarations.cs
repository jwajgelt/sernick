namespace sernick.Parser.Ast;

public sealed record ConstDeclaration(Identifier name,
    DeclaredType? DeclaredType,
    Expression? initValue) : Declaration;

public sealed record VariableDeclaration(Identifier name,
    DeclaredType? declaredType,
    Expression? initValue) : Declaration;

public sealed record FunctionArgumentDeclaration(Identifier name,
    DeclaredType declaredType,
    LiteralValue? defaultValue) : Declaration;

public record FunctionDefinition(Identifier name,
    IEnumerable<FunctionArgumentDeclaration> argsDeclaration,
    DeclaredType returnType,
    CodeBlock functionBody) : Declaration;
