namespace sernick.Ast.Nodes;

public sealed record VariableDeclaration(Identifier Name,
    Type? Type,
    Expression? InitValue,
    bool IsConst) : Declaration;

public sealed record FunctionParameterDeclaration(Identifier Name,
    Type Type,
    LiteralValue? DefaultValue) : Declaration;

public record FunctionDefinition(Identifier Name,
    IEnumerable<FunctionParameterDeclaration> Parameters,
    Type ReturnType,
    CodeBlock Body) : Declaration;
