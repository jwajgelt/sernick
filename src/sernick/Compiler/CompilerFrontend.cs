namespace sernick.Compiler;

using Ast.Analysis.FunctionContextMap;
using Ast.Analysis.NameResolution;
using Ast.Analysis.VariableAccess;
using Ast.Nodes;
using Ast.Nodes.Conversion;
using Common.Dfa;
using Common.Regex;
using Diagnostics;
using Function;
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
    public static void Process(IInput input, IDiagnostics diagnostics)
    {
        var lexer = lazyLexer.Value;
        var tokens = lexer.Process(input, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);
        var parseLeaves = tokens.ProcessIntoLeaves();
        var parser = lazyParser.Value;
        var parseTree = parser.Process(parseLeaves, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);

        AstNode ast;
        try
        {
            ast = AstNode.From(parseTree);
        }
        catch (UnknownTypeException e)
        {
            diagnostics.Report(new UnknownTypeError(e.Name, e.LocationRange));
            throw new CompilationException();
        }

        var nameResolution = NameResolutionAlgorithm.Process(ast, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);
        _ = FunctionContextMapProcessor.Process(ast, nameResolution, new FunctionFactory());
        _ = VariableAccessMapPreprocess.Process(ast, nameResolution);
        // commented since it throws NotImplemented, and will need merging anyway
        // var functionCodeTreeMap = FunctionCodeTreeMapGenerator.Process(ast,
        // root => ControlFlowAnalyzer.UnravelControlFlow(root, nameResolution, functionContextMap, SideEffectsAnalyzer.PullOutSideEffects));
    }

    private static readonly Lazy<ILexer<LexicalGrammarCategory>> lazyLexer = new(() =>
    {
        var grammar = new LexicalGrammar();
        var grammarDict = grammar.GenerateGrammar();
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
