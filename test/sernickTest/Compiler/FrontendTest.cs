using sernick.Common.Dfa;
using sernick.Common.Regex;
using sernick.Grammar.Lexicon;
using sernick.Tokenizer.Lexer;
using sernick.Utility;
using sernickTest.Diagnostics;

namespace sernickTest.Compiler;

public static class FrontendTest
{
    public static FakeDiagnostics Compile(string fileName)
    {
        var input = FileUtility.ReadFile(fileName);
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
        var tokens = lexer.Process(input, diagnostics).ToList();

        return diagnostics;
    }
}
