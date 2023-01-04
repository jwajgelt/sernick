namespace sernickTest.Grammar;

using Common.Dfa.Helpers;
using sernick.Common.Dfa;
using sernick.Grammar.Lexicon;

public class TestGrammar
{
    private static readonly string[] keywords = { "loop", "var", "const", "fun", "break", "continue", "return", "struct" };
    private static readonly string[] notKeywords = { "Loop", "Var", "CONST", "function", "breaking", "go", "ret", "type", "class" };

    private static readonly string[] integerLiterals = { "123", "2", "137", "999000999" };
    private static readonly string[] notIntegerLiterals = { "123.45", "21.3", "7,99", "999,99$", "0.00", "0/0" };

    private static readonly string[] booleanLiterals = { "true", "false" };
    private static readonly string[] notBooleanLiterals = { "True", "False", "Truth", "Fals" };

    private static readonly string[] comments = { "// single-line comment", @"/* multi
    line
    comment */", "/* multi-line comment in one line */" };
    private static readonly string[] notComments = { "/ missing second /",
     @"//* multi
       line with extra second/
       so invalid */",
     "* missing / at the beginning */",
     "# python-style comment",
     "just random text",
     "        " };

    private static readonly string[] typeNames = { "Int", "Bool", "Unit", "CustomTypeName", "A", "B", "Z", "ABBA", "UJ" };
    private static readonly string[] variableNames = { "varia", "ble", "name", "tcs", "mess", "graphQL", };

    private static readonly string[] operators = { "+", "-", "||", "&&" };
    /// <summary> maybe some of these will be operators in the future, but not now </summary>
    private static readonly string[] notOperators = { "*", "/", "^", "//", "pow", "$", "(", ")", "{}" };

    private static readonly string[] whitespaces = { " ", "     ", "       ", "    ", };
    private static readonly string[] notWhitespaces = { "", "123", ";", "#" };

    private static readonly string[] colon = { ":", };
    private static readonly string[] semicolon = { ";" };
    private static readonly string[] bracesAndParentheses = { "{", "}", ")", "(" };
    private static readonly string[] notBracesNotParentheses = { "[", "]", @"\", "123", "/" };

    private static void TestCategories(
        IEnumerable<LexicalGrammarCategory> categoriesWhichShouldAccept,
        IEnumerable<string> goodExamples,
        IEnumerable<string> badExamples
    )
    {
        var grammar = LexicalGrammar.GenerateGrammar();

        foreach (var grammarCategoryKey in categoriesWhichShouldAccept)
        {
            var dfa = RegexDfa<char>.FromRegex(grammar[grammarCategoryKey].Regex);
            foreach (var word in goodExamples)
            {
                var state = dfa.Transition(dfa.Start, word);
                Assert.True(dfa.Accepts(state));
            }

            foreach (var word in badExamples)
            {
                var state = dfa.Transition(dfa.Start, word);
                Assert.False(dfa.Accepts(state));
            }
        }
    }

    [Fact]
    public void Grammar_categories_priorities_are_distinct()
    {
        var priorities = LexicalGrammar.GenerateGrammar().Values.ToList().ConvertAll(grammarEntry => grammarEntry.Category);
        // hash set size == list size => no two list elements are equal
        Assert.True(priorities.Count == priorities.ToHashSet().Count);
    }

    [Fact]
    public void Test_keywords_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.Keywords },
         keywords,
         notKeywords);
    }

    [Fact]
    public void Test_comments_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.Comments },
         comments,
         notComments);
    }

    [Fact]
    public void Test_braces_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.BracesAndParentheses },
         bracesAndParentheses,
         notBracesNotParentheses);
    }

    [Fact]
    public void Test_whitespaces_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.Whitespaces },
         whitespaces,
         notWhitespaces);
    }

    [Fact]
    public void Test_colon_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.Colon },
         colon,
         semicolon);
    }

    [Fact]
    public void Test_semicolon_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.Semicolon },
         semicolon,
         bracesAndParentheses);
    }

    [Fact]
    public void Test_type_identifiers_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.TypeIdentifiers },
         typeNames,
         variableNames);
    }

    [Fact]
    public void Test_variable_identifiers_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.VariableIdentifiers },
         variableNames,
         typeNames);
    }

    [Fact]
    public void Test_operators_category()
    {
        TestCategories(new[] { LexicalGrammarCategory.Operators },
         operators,
         notOperators);
    }

    [Fact]
    public void Test_literals_category_integers()
    {
        TestCategories(new[] { LexicalGrammarCategory.Literals },
        integerLiterals,
        notIntegerLiterals);
    }

    [Fact]
    public void Test_literals_category_booleans()
    {
        TestCategories(new[] { LexicalGrammarCategory.Literals },
         booleanLiterals,
         notBooleanLiterals);
    }
}
