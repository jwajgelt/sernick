namespace sernick.Compiler;

using Common.Dfa;
using Common.Regex;
using Diagnostics;
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
            .Where(token => !token.Category.Equals(LexicalGrammarCategoryType.Whitespaces)) // strip whitespace
            .Select(token =>
                new ParseTreeLeaf<Symbol>(new Terminal(token.Category, token.Text), token.Start, token.End));
        var parser = Parser<Symbol>.FromGrammar(SernickGrammar.Create());
        var parseTree = parser.Process(parseLeaves, diagnostics);
        ThrowIfErrorsOccurred(diagnostics);
    }

    private static ILexer<LexicalGrammarCategoryType> PrepareLexer()
    {
        var grammar = new LexicalGrammar();
        var grammarDict = grammar.GenerateGrammar();
        var categoryDfas =
            grammarDict.ToDictionary(
                e => e.Key,
                e => (IDfa<Regex<char>, char>)RegexDfa<char>.FromRegex(e.Value.Regex)
            );
        return new Lexer<LexicalGrammarCategoryType, Regex<char>>(categoryDfas);
    }

    private static void ThrowIfErrorsOccurred(IDiagnostics diagnostics)
    {
        if (diagnostics.DidErrorOccur)
        {
            throw new CompilationException();
        }
    }
}
