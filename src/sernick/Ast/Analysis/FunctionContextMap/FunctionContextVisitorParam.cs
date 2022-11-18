namespace sernick.Ast.Analysis.FunctionContextMap;

using Nodes;

public record struct FunctionContextVisitorParam(FunctionDefinition? EnclosingFunction = null);
