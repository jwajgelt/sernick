namespace sernickTest.Compiler.Helpers;

using Diagnostics;
using sernick.Common.Dfa;
using sernick.Common.Regex;
using sernick.Grammar.Lexicon;
using sernick.Tokenizer.Lexer;
using sernick.Utility;

public static class FrontendHelper
{
    public static FakeDiagnostics Compile(this string fileName)
    {
        var input = fileName.ReadFile();
        var diagnostics = new FakeDiagnostics();

        var grammar = new LexicalGrammar().GenerateGrammar();
        var categoryDfas =
            grammar.ToDictionary(
                grammarEntry => grammarEntry.Key,
                grammarEntry => (IDfa<Regex<char>, char>)RegexDfa<char>.FromRegex(grammarEntry.Value.Regex)
            );
        var lexer = new Lexer<LexicalGrammarCategoryType, Regex<char>>(categoryDfas);

        /*
         * ToList() is needed to force evaluation of the tokens, so the whole lexer.Process() computes
         * and diagnostics are reported
         */
        _ = lexer.Process(input, diagnostics).ToList();

        return diagnostics;
    }
}
