namespace sernick.Grammar;
using sernick.Tokenizer.Regex;

using CategoryItems = Dictionary<string, string>;

public class GrammarEntry
{
    public IGrammarCategory Category { get; init; }
    public Regex Regex { get; init; }

    private static Regex createUnionRegex(CategoryItems categoryItems)
    {
        return string.Join("|", categoryItems.Values).ToRegex();

    }

    public GrammarEntry(IGrammarCategory category, CategoryItems rules)
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
    //
    // You could use e.g. https://regex101.com to test if the given regex is OK 

    private readonly GrammarEntry bracesAndParentheses = new GrammarEntry(new BraceCategory(), new CategoryItems()
    {
        ["leftBrace"] = "{",
        ["rightBrace"] = "}",
        ["leftParentheses"] = @"\)",
        ["rightParentheses"] = @"\)",
    });

    private readonly GrammarEntry semicolon = new GrammarEntry(new LineDelimiterCategory(), new CategoryItems()
    {
        ["semicolon"] = ";"
    });

    private readonly GrammarEntry colon = new GrammarEntry(new ColonCategory(), new CategoryItems()
    {
        ["colon"] = ":"
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
        ["typeNames"] = "[[:upper:]][[:alnum:]]*",
    });

    private readonly GrammarEntry operators = new GrammarEntry(new OperatorCategory(), new CategoryItems()
    {
        ["plus"] = @"\+",
        ["minus"] = "-",
        ["shortCircuitOr"] = @"\|\|",
        ["shortCircuitAnd"] = "&&"
    });

    private readonly GrammarEntry whitespaces = new GrammarEntry(new WhitespaceCategory(), new CategoryItems()
    {
        ["blankCharacter"] = "[[:space:]]+"
    });

    private readonly GrammarEntry variableIdentifiers = new GrammarEntry(new VariableIdentifierCategory(), new CategoryItems()
    {

        ["variableNames"] = "[[:lower:]][[:alnum:]]*",
    });

    private readonly GrammarEntry literals = new GrammarEntry(new LiteralsCategory(), new CategoryItems()
    {
        ["integers"] = "[[:digit:]]+",
        ["true"] = "true",
        ["false"] = "false",
    });

    private readonly GrammarEntry comments = new GrammarEntry(new LiteralsCategory(), new CategoryItems()
    {
        ["singleLineComment"] = "//.*",
        ["multiLineComment"] = @"/\*(.|[[:space:]])*\*/"
    });

    // TODO test that every category, returned from GenerateGrammar, has distinct priority
    public List<GrammarEntry> generateGrammar()
    {
        return new List<GrammarEntry>() {
            bracesAndParentheses,
            semicolon,
            colon,
            keywords,
            typeIdentifiers,
            operators,
            whitespaces,
            variableIdentifiers,
            literals,
            comments
        };

    }
}
