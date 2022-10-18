namespace sernickTest.Grammar;

using sernick.Grammar;
using sernick.Tokenizer.Dfa;
using Tokenizer.Dfa.Helpers;

public class TestGrammar
{
    private readonly string[] keywords = { "loop", "var", "const", "fun", "break", "continue", "return" };
    private readonly string[] notKeywords = { "Loop", "Var", "CONST", "function", "breaking", "go", "ret" };

    private readonly string[] integerLiterals = { "123", "2", "137", "999000999" };
    private readonly string[] notIntegerLiterals = { "123.45", "21.3", "7,99", "999,99$", "0.00", "0/0" };

    private readonly string[] booleanLiterals = { "true", "false" };
    private readonly string[] notBooleanLiterals = { "True", "False", "Truth", "Fals" };

    private readonly string[] comments = { "// single-line comment", @"/* multi
    line
    comment */", "/* multi-line comment in one line */" };
    private readonly string[] notComments = { "/ missing second /",
     @"//* multi
       line with extra second/
       so invalid */",
     "* missing / at the beginning */",
     "# python-style comment",
     "just random text",
     "        " };

    private readonly string[] typeNames = { "Int", "Bool", "CustomTypeName", "A", "B", "Z", "ABBA", "UJ" };
    private readonly string[] variableNames = { "varia", "ble", "name", "tcs", "mess", "graphQL", };

    private readonly string[] operators = { "+", "-", "||", "&&" };
    /// <summary> maybe some of these will be operators in the future, but not now </summary>
    private readonly string[] notOperators = { "*", "/", "^", "//", "pow", "$", "(", ")", "{}" };

    private readonly string[] whitespaces = { " ", "     ", "       ", "    ", };
    private readonly string[] notWhitespaces = { "", "123", ";", "#" };

    private readonly string[] colon = { ":", };
    private readonly string[] semicolon = { ";" };
    private readonly string[] bracesAndParentheses = { "{", "}", ")", "(" };
    private readonly string[] notBracesNotParentheses = { "[", "]", @"\", "123", "/" };

    private static void testCategories(
        IEnumerable<GrammarCategoryType> categoriesWhichShouldAccept,
        IEnumerable<string> goodExamples,
        IEnumerable<string> badExamples
    )
    {
        var grammar = new Grammar().GenerateGrammar();

        foreach (var grammarCategoryKey in categoriesWhichShouldAccept)
        {
            var dfa = new RegexDfa(grammar[grammarCategoryKey].Regex);
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
        var grammar = new Grammar();
        var priorities = grammar.GenerateGrammar().Values.ToList().ConvertAll(grammarEntry => grammarEntry.Category.Priority);
        // hash set size == list size => no two list elements are equal
        Assert.True(priorities.Count == priorities.ToHashSet().Count);
    }

    [Fact]
    public void Test_keywords_category()
    {
        testCategories(new[] { GrammarCategoryType.Keywords },
         keywords,
         notKeywords);
    }

    [Fact]
    public void Test_comments_category()
    {
        testCategories(new[] { GrammarCategoryType.Comments },
         comments,
         notComments);
    }

    [Fact]
    public void Test_braces_category()
    {
        testCategories(new[] { GrammarCategoryType.BracesAndParentheses },
         bracesAndParentheses,
         notBracesNotParentheses);
    }

    [Fact]
    public void Test_whitespaces_category()
    {
        testCategories(new[] { GrammarCategoryType.Whitespaces },
         whitespaces,
         notWhitespaces);
    }

    [Fact]
    public void Test_colon_category()
    {
        testCategories(new[] { GrammarCategoryType.Colon },
         colon,
         semicolon);
    }

    [Fact]
    public void Test_semicolon_category()
    {
        testCategories(new[] { GrammarCategoryType.Semicolon },
         semicolon,
         bracesAndParentheses);
    }

    [Fact]
    public void Test_type_identifiers_category()
    {
        testCategories(new[] { GrammarCategoryType.TypeIdentifiers },
         typeNames,
         variableNames);
    }

    [Fact]
    public void Test_variable_identifiers_category()
    {
        testCategories(new[] { GrammarCategoryType.VariableIdentifiers },
         variableNames,
         typeNames);
    }

    [Fact]
    public void Test_operators_category()
    {
        testCategories(new[] { GrammarCategoryType.Operators },
         operators,
         notOperators);
    }

    [Fact]
    public void Test_literals_category_integers()
    {
        testCategories(new[] { GrammarCategoryType.Literals },
        integerLiterals,
        notIntegerLiterals);
    }

    [Fact]
    public void Test_literals_category_booleans()
    {
        testCategories(new[] { GrammarCategoryType.Literals },
         booleanLiterals,
         notBooleanLiterals);
    }
}
