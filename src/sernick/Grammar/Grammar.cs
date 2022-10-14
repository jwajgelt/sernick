namespace sernick.Grammar;
using sernick.Tokenizer.Regex;

using CategoryItems = Dictionary<string, string>;

public class GrammarEntry
{
    public GrammarCategory Category;
    public Regex Regex;


    private static Regex createUnionRegex(CategoryItems categoryItems)
    {
        string unionRegexAsString = categoryItems.Values.Aggregate("", (partialResult, currentRegex) => partialResult + "|" + currentRegex);
        return StringToRegex.ToRegex(unionRegexAsString);
    }

    public GrammarEntry(GrammarCategory category, CategoryItems rules)
    {
        Category = category;
        Regex = createUnionRegex(rules);
    }
}



public class Grammar
{
    // !!! IMPORTANT !!!
    // make sure to only use POSIX-Extended Regular Expressions
    // as specified here: https://en.m.wikibooks.org/wiki/Regular_Expressions/POSIX-Extended_Regular_Expressions
    // Otherwise our StringToRegex might not be able to parse it

    private GrammarEntry bracesAndParentheses = new GrammarEntry(new BraceCategory(), new CategoryItems()
    {
        ["leftBrace"] = "{",
        ["rightBrace"] = "}",
        ["leftParentheses"] = @"\)",
        ["rightParentheses"] = @"\)",
    });

    private GrammarEntry lineDelimiters = new GrammarEntry(new LineDelimiterCategory(), new CategoryItems()
    {
        ["semicolon"] = ";"
    });


    private readonly GrammarEntry keywords = new GrammarEntry(new KeywordCategory(), new CategoryItems()
    {
        ["var"] = "var",
        ["const"] = "const",
        ["function"] = "fun",
        ["loop"] = "loop",
        ["break"] = "break",
        ["continue"] = "continue",
        ["return"] = "return",
    });

    private readonly GrammarEntry typeIdentifiers = new GrammarEntry(new TypeIdentifierCategory(), new CategoryItems()
    {
        ["Int"] = "Int",
        ["Bool"] = "Bool",
        ["typesNames"] = "[[:upper:]][:alnum:]*",
    });

    private readonly GrammarEntry operators = new GrammarEntry(new OperatorCategory(), new CategoryItems()
    {
        ["plus"] = "+",
        ["minus"] = "-",
        ["shortCircuitOr"] = "||",
        ["shortCircuitAnd"] = "&&"
    });

    private readonly GrammarEntry whitespaces = new GrammarEntry(new WhitespaceCategory(), new CategoryItems()
    {
        ["blankCharacter"] = ":space:"
    });

    private readonly GrammarEntry variableIdentifiers = new GrammarEntry(new VariableIdentifierCategory(), new CategoryItems()
    {

        ["variableNames"] = "[[:upper:]|[:lower:]][:alnum:]*",
    });

    private readonly GrammarEntry literals = new GrammarEntry(new LiteralsCategory(), new CategoryItems()
    {
        ["integers"] = "[:digit:]+",
        ["true"] = "true",
        ["false"] = "false",
    });

    private readonly GrammarEntry comments = new GrammarEntry(new LiteralsCategory(), new CategoryItems()
    {
        ["singleLineComment"] = "//.*$",
        ["multiLineComment"] = @"//\*.*\*/"
    });




    public List<GrammarEntry> generateGrammar()
    {
        return new List<GrammarEntry>() {
            bracesAndParentheses,
            lineDelimiters,
            keywords,
            typeIdentifiers,
            operators,
            whitespaces,
            variableIdentifiers,
            literals,
        };


    }
}
