using sernick.Grammar;
using sernick.Tokenizer.Regex;
using sernick.Tokenizer.Dfa;
namespace sernickTest.Grammar;

public class TestGrammar
{

    private string[] keywords = new string[] { "loop", "var", "const", "fun", "break", "continue", "return" };
    private string[] notKeywords = new string[] { "Loop", "Var", "CONST", "function", "breaking", "go", "ret" };


    private string[] integerLiterals = new string[] { "123", "2", "137", "999000999" };
    private string[] notIntegerLiterals = new string[] { "123.45", "21.3", "7,99", "999,99$", "0.00", "0/0" };

    private string[] booleanLiterals = new string[] { "true", "false" };
    private string[] notBooleanLiterals = new string[] { "True", "False", "1", "0", "Truth", "Fals" };

    private string[] comments = new string[] { "// single-line comment", @"/* multi
    line
    comment */", "/* multi-line comment in one line */" };
    private string[] notComments = new string[] { "/ missing second /",
     @"//* multi
       line with extra second/
       so invalid */",
     "* missing / at the beginning */",
     "# python-style comment",
     "just random text",
     "        " };

    private string[] typeNames = new string[] { "Int", "Bool", "CustomTypeName", "A", "B", "Z", "ABBA", "UJ" };
    private string[] variableNames = new string[] { "varia", "ble", "name", "tcs", "mess", "graphQL", };


    private string[] operators = new string[] { "+", "-", "||", "&&" };
    /// <summary> maybe some of these will be operators in the future, but not now </summary>
    private string[] notOperators = new string[] { "*", "/", "^", "//", "pow", "$", "(", ")", "{}" };

    private string[] whitespaces = new string[] { " ", "     ", "       ", "    ", };
    private string[] notWhitespaces = new string[] { "", "123", ";", "#" };

    private string[] colon = new string[] { ":", };
    private string[] semicolon = new string[] { ";" };
    private string[] bracesAndParentheses = new string[] { "{", "}", ")", "(" };
    private string[] notBracesNotParentheses = new string[] { "[", "]", @"\", "123", "/" };


    public void testCategories(
        IEnumerable<GrammarCategoryType> categoriesWhichShouldAccept,
        IEnumerable<string> goodExamples,
        IEnumerable<string> badExamples
    )
    {
        var grammar = new sernick.Grammar.Grammar().generateGrammar();
        var allGrammarCategories = (GrammarCategoryType[])Enum.GetValues(typeof(GrammarCategoryType));

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
        var grammar = new sernick.Grammar.Grammar();
        var priorities = grammar.generateGrammar().Values.ToList().ConvertAll((grammarEntry) => grammarEntry.Category.Priority);
        // hash set size == list size => no two list elements are equal
        Assert.True(priorities.Count == priorities.ToHashSet().Count);
    }

    [Fact]
    public void Test_keywords_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Keywords },
         keywords,
         notKeywords);
    }

    [Fact]
    public void Test_comments_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Comments },
         comments,
         notComments);
    }

    [Fact]
    public void Test_braces_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.BracesAndParentheses },
         bracesAndParentheses,
         notBracesNotParentheses);
    }

    [Fact]
    public void Test_whitespaces_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Whitespaces },
         whitespaces,
         notWhitespaces);
    }

    [Fact]
    public void Test_colon_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Colon },
         colon,
         semicolon);
    }

    [Fact]
    public void Test_semicolon_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Semicolon },
         semicolon,
         bracesAndParentheses);
    }

    [Fact]
    public void Test_type_identifiers_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.TypeIdentifiers },
         typeNames,
         variableNames);
    }

    [Fact]
    public void Test_variable_identifiers_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.VariableIdentifiers },
         variableNames,
         typeNames);
    }

    [Fact]
    public void Test_operators_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Operators },
         operators,
         notOperators);
    }

    [Fact]
    public void Test_literals_category()
    {
        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Literals },
         booleanLiterals,
         notBooleanLiterals);

        testCategories(new GrammarCategoryType[] { GrammarCategoryType.Literals },
         integerLiterals,
         notIntegerLiterals);
    }

}