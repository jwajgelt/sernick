namespace sernick.Compiler;

using Ast.Analysis.FunctionContextMap;
using Ast.Analysis.NameResolution;
using Ast.Nodes;
using Common.Dfa;
using Common.Regex;
using Diagnostics;
using Function;
using Grammar.Lexicon;
using Grammar.Syntax;
using Input;
using Parser;
using Parser.ParseTree;
using Tokenizer.Lexer;

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
        var lexer = PrepareLexer();
        var tokens = lexer.Process(input, diagnostics);
        var parseLeaves = tokens
            .Where(token => !token.Category.Equals(LexicalGrammarCategory.Whitespaces)) // strip whitespace
            .Where(token => !token.Category.Equals(LexicalGrammarCategory.Comments)) // ignore comments
            .Select(token =>
                new ParseTreeLeaf<Symbol>(new Terminal(token.Category, token.Text), token.LocationRange));
        var parser = Parser<Symbol>.FromGrammar(SernickGrammar.Create(), new NonTerminal(NonTerminalSymbol.Start));
        var parseTree = parser.Process(parseLeaves, diagnostics);
        var ast = AstNode.From(parseTree);
        var nameResolution = NameResolutionAlgorithm.Process(ast, diagnostics);
        var functionContextMap = FunctionContextMapProcessor.Process(ast, nameResolution, new FunctionFactory());
        ThrowIfErrorsOccurred(diagnostics);
    }

    private static ILexer<LexicalGrammarCategory> PrepareLexer()
    {
        var grammar = new LexicalGrammar();
        var grammarDict = grammar.GenerateGrammar();
        var categoryDfas =
            grammarDict.ToDictionary(
                e => e.Key,
                e => RegexDfa<char>.FromRegex(e.Value.Regex)
            );
        return new Lexer<LexicalGrammarCategory, Regex<char>>(categoryDfas);
    }

    private static void ThrowIfErrorsOccurred(IDiagnostics diagnostics)
    {
        if (diagnostics.DidErrorOccur)
        {
            throw new CompilationException();
        }
    }
}
