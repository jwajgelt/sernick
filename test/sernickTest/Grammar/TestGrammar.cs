using sernick.Grammar;
using sernick.Tokenizer.Regex;
using sernick.Tokenizer.Dfa;
namespace sernickTest.Grammar;

public class TestGrammar
{

    private string[] keywords = new string[] { "loop", "var", "const", "fun", "break", "continue", "return" };
    private string[] integerLiterals = new string[] { "123", "2", "137", "999000999" };
    private string[] booleanLiterals = new string[] { "true", "false" };
    private string[] comments = new string[] { "// single-line comment", @"/* multi
    line
    comment */", "/* multi-line comment in one line */" };

    private string[] typeNames = new string[] { "Int", "Bool", "CustomTypeName", "A", "B", "Z", "ABBA", "UJ" };
    private string[] variableNames = new string[] { "varia", "ble", "name", "tcs", "mess", "graphQL", };

    private string[] operators = new string[] { "+", "-", "||", "&&" };

    private string[] blankCharacters = new string[] { " ", "     ", "       ", };

    private string[] colon = new string[] { ":", };
    private string[] semicolon = new string[] { ";" };
    private string[] bracesAndParentheses = new string[] { "{", "}", ")", "(" };


    public void testCategoryAcceptingStrings(
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
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.Keywords },
         keywords,
         bracesAndParentheses);
    }

    [Fact]
    public void Test_comments_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.Comments },
         comments,
         bracesAndParentheses); // add more test cases here?
    }

    [Fact]
    public void Test_braces_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.BracesAndParentheses },
         bracesAndParentheses,
         blankCharacters);
    }

    [Fact]
    public void Test_colon_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.Colon },
         colon,
         semicolon);
    }

    [Fact]
    public void Test_semicolon_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.Semicolon },
         semicolon,
         bracesAndParentheses);
    }

    [Fact]
    public void Test_type_identifiers_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.TypeIdentifiers },
         typeNames,
         variableNames);
    }

    [Fact]
    public void Test_variable_identifiers_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.VariableIdentifiers },
         variableNames,
         typeNames);
    }

    [Fact]
    public void Test_operators_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.Operators },
         operators,
         integerLiterals);
    }

    [Fact]
    public void Test_literals_category()
    {
        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.Literals },
         booleanLiterals,
         bracesAndParentheses);

        testCategoryAcceptingStrings(new GrammarCategoryType[] { GrammarCategoryType.Literals },
         integerLiterals,
         comments);
    }

}