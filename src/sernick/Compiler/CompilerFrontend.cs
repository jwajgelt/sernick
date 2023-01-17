namespace sernick.Compiler;

using Ast.Analysis;
using Ast.Analysis.CallGraph;
using Ast.Analysis.NameResolution;
using Ast.Analysis.StructProperties;
using Ast.Analysis.TypeChecking;
using Ast.Analysis.VariableAccess;
using Ast.Analysis.VariableInitialization;
using Ast.Nodes;
using Common.Dfa;
using Common.Regex;
using Diagnostics;
using Grammar.Lexicon;
using Grammar.Syntax;
using Input;
using Parser;
using Parser.ParseTree;
using Tokenizer;
using Tokenizer.Lexer;
using Utility;

public static class CompilerFrontend
{
    /// <summary>
    /// This method performs the frontend stage of the compilation, reporting diagnostics if needed.
    /// It returns void for now (will be changed in the future, once we have a complete implementation)
    /// </summary>
    /// <param name="input"></param>
    /// <param name="diagnostics"></param>
    public static CompilerFrontendResult Process(IInput input, IDiagnostics diagnostics)
    {
        var lexer = lazyLexer.Value;
        var tokens = lexer.Process(input, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);
        var parseLeaves = tokens.ProcessIntoLeaves();
        var parser = lazyParser.Value;
        var parseTree = parser.Process(parseLeaves, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);

        var ast = AstNode.From(parseTree);

        var nameResolution = NameResolutionAlgorithm.Process(ast, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);
        var typeCheckingResult = TypeChecking.CheckTypes(ast, nameResolution, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);
        var callGraph = CallGraphBuilder.Process(ast, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(ast, nameResolution, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);
        InstallBuiltinFunctions(variableAccessMap);

        if (ast is not FunctionDefinition main)
        {
            throw new CompilationException("Program should parse to a `main` function definition");
        }

        VariableInitializationAnalyzer.Process(main, variableAccessMap, nameResolution, callGraph, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);

        var structProperties = StructPropertiesProcessor.Process(ast, nameResolution, diagnostics);

        return new CompilerFrontendResult(ast, nameResolution, structProperties, typeCheckingResult, callGraph, variableAccessMap);
    }

    private static void InstallBuiltinFunctions(VariableAccessMap variableAccessMap)
    {
        // add built-in functions to function analysis structures
        foreach (var externalFunction in ExternalFunctionsInfo.ExternalFunctions)
        {
            variableAccessMap.AddFun(externalFunction.Definition);
        }
    }

    private static readonly Lazy<ILexer<LexicalGrammarCategory>> lazyLexer = new(() =>
    {
        var grammarDict = LexicalGrammar.GenerateGrammar();
        var categoryDfas =
            grammarDict.ToDictionary(
                e => e.Key,
                e => RegexDfa<char>.FromRegex(e.Value.Regex)
            );
        return new Lexer<LexicalGrammarCategory, Regex<char>>(categoryDfas);
    });

    private static readonly Lazy<Parser<Symbol>> lazyParser = new(() =>
    {
        var grammar = SernickGrammar.Create();
        return Parser<Symbol>.FromGrammar(grammar, new NonTerminal(NonTerminalSymbol.Start));
    });

    private static void ThrowIfErrorsOccurred(IDiagnostics diagnostics)
    {
        if (diagnostics.DidErrorOccur)
        {
            throw new CompilationException();
        }
    }

    private static IEnumerable<ParseTreeLeaf<Symbol>> ProcessIntoLeaves(
        this IEnumerable<Token<LexicalGrammarCategory>> tokens)
    {
        return tokens
            .Where(token => !token.Category.Equals(LexicalGrammarCategory.Whitespaces)) // strip whitespace
            .Where(token => !token.Category.Equals(LexicalGrammarCategory.Comments)) // ignore comments
            .Select(token =>
                new ParseTreeLeaf<Symbol>(new Terminal(token.Category, token.Text), token.LocationRange));
    }
}

public sealed record UnknownTypeError(string Name, Range<ILocation> LocationRange) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Unknown type name \"{Name}\" at ${LocationRange.Start}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
