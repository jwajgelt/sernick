namespace sernick.Compiler;

using Ast.Analysis.CallGraph;
using Ast.Analysis.NameResolution;
using Ast.Analysis.StructProperties;
using Ast.Analysis.TypeChecking;
using Ast.Analysis.VariableAccess;
using Ast.Nodes;

public record CompilerFrontendResult(AstNode AstRoot,
    NameResolutionResult NameResolution, StructProperties StructProperties, TypeCheckingResult TypeCheckingResult,
    CallGraph CallGraph, VariableAccessMap VariableAccessMap);
