using sernick.Common.Dfa;
using sernick.Common.Regex;
using sernick.Grammar.Lexicon;
using sernick.Tokenizer.Lexer;

namespace sernick.Compiler;

using Diagnostics;
using Input;

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
        // force c# to evaluate the IEnumerable
        tokens.ToArray();
        ThrowIfErrorsOccurred(diagnostics);
    }

    private static ILexer<LexicalGrammarCategoryType> PrepareLexer()
    {
        var grammar = new LexicalGrammar();
        var grammarDict = grammar.GenerateGrammar();
        var categoryDfas =
            grammarDict.ToDictionary(
                e => e.Key,
                e => (IDfa<Regex>)RegexDfa.FromRegex(e.Value.Regex)
            );
        return new Lexer<LexicalGrammarCategoryType, Regex>(categoryDfas);
    }

    private static void ThrowIfErrorsOccurred(IDiagnostics diagnostics)
    {
        if (diagnostics.DidErrorOccur)
        {
            throw new CompilationException();
        }
    }
}
