namespace sernick.Grammar.Lexicon;

using Common.Regex;
using Tokenizer.Regex;

using CategoryItems = Dictionary<string, string>;

public enum LexicalGrammarCategoryType
{
    Comments,
    BracesAndParentheses,
    Semicolon,
    Colon,
    Comma,
    Keywords,
    TypeIdentifiers,
    VariableIdentifiers,
    Operators,
    Whitespaces,
    Literals,
}

public class LexicalGrammarEntry
{
    public ILexicalGrammarCategory Category { get; }
    public Regex<char> Regex { get; }

    private static Regex<char> CreateUnionRegex(CategoryItems categoryItems)
    {
        return string.Join("|", categoryItems.Values).ToRegex();
    }

    public LexicalGrammarEntry(ILexicalGrammarCategory category, CategoryItems rules)
    {
        Category = category;
        Regex = CreateUnionRegex(rules);
    }
}

/// <summary>
/// This class defines which combinations of ASCII characters constitute different lexical tokens.
/// </summary>
public class LexicalGrammar
{
    // !!! IMPORTANT !!!
    // make sure to only use POSIX-Extended Regular Expressions
    // as specified here: https://en.m.wikibooks.org/wiki/Regular_Expressions/POSIX-Extended_Regular_Expressions
    // Otherwise our StringToRegex might not be able to parse it
    //
    // You could use e.g. https://regex101.com to test if the given regex is OK 

    private readonly LexicalGrammarEntry bracesAndParentheses = new(new BraceCategory(), new CategoryItems
    {
        ["leftBrace"] = "{",
        ["rightBrace"] = "}",
        ["leftParentheses"] = @"\(",
        ["rightParentheses"] = @"\)",
    });

    private readonly LexicalGrammarEntry semicolon = new(new LineDelimiterCategory(), new CategoryItems
    {
        ["semicolon"] = ";"
    });

    private readonly LexicalGrammarEntry colon = new(new ColonCategory(), new CategoryItems
    {
        ["colon"] = ":"
    });
    
    private readonly LexicalGrammarEntry comma = new(new CommaCategory(), new CategoryItems
    {
        ["comma"] = ","
    });

    private readonly LexicalGrammarEntry keywords = new(new KeywordCategory(), new CategoryItems
    {
        ["var"] = "var",
        ["const"] = "const",
        ["function"] = "fun",
        ["loop"] = "loop",
        ["break"] = "break",
        ["continue"] = "continue",
        ["return"] = "return",
    });

    private readonly LexicalGrammarEntry typeIdentifiers = new(new TypeIdentifierCategory(), new CategoryItems
    {
        ["Int"] = "Int",
        ["Bool"] = "Bool",
        ["typeNames"] = "[[:upper:]][[:alnum:]]*",
    });

    private readonly LexicalGrammarEntry operators = new(new OperatorCategory(), new CategoryItems
    {
        ["plus"] = @"\+",
        ["minus"] = "-",
        ["shortCircuitOr"] = @"\|\|",
        ["shortCircuitAnd"] = "&&",
        ["assignment"] = "=",
        ["equality"] = "==",
        ["greater"] = ">",
        ["less"] = "<",
        ["greaterOrEqual"] = ">=",
        ["lessOrEqual"] = "<="
    });

    private readonly LexicalGrammarEntry whitespaces = new(new WhitespaceCategory(), new CategoryItems
    {
        ["blankCharacter"] = "[[:space:]]+"
    });

    private readonly LexicalGrammarEntry variableIdentifiers = new(new VariableIdentifierCategory(), new CategoryItems
    {

        ["variableNames"] = "[[:lower:]][[:alnum:]]*",
    });

    private readonly LexicalGrammarEntry literals = new(new LiteralsCategory(), new CategoryItems
    {
        ["integers"] = "[[:digit:]]+",
        ["true"] = "true",
        ["false"] = "false",
    });

    private readonly LexicalGrammarEntry comments = new(new CommentCategory(), new CategoryItems
    {
        ["singleLineComment"] = "//.*",
        ["multiLineComment"] = @"/\*(.|[[:space:]])*\*/"
    });

    public Dictionary<LexicalGrammarCategoryType, LexicalGrammarEntry> GenerateGrammar()
    {
        return new Dictionary<LexicalGrammarCategoryType, LexicalGrammarEntry>
        {
            [LexicalGrammarCategoryType.Colon] = colon,
            [LexicalGrammarCategoryType.Semicolon] = semicolon,
            [LexicalGrammarCategoryType.Comma] = comma,
            [LexicalGrammarCategoryType.BracesAndParentheses] = bracesAndParentheses,
            [LexicalGrammarCategoryType.Comments] = comments,
            [LexicalGrammarCategoryType.Literals] = literals,
            [LexicalGrammarCategoryType.Operators] = operators,
            [LexicalGrammarCategoryType.TypeIdentifiers] = typeIdentifiers,
            [LexicalGrammarCategoryType.VariableIdentifiers] = variableIdentifiers,
            [LexicalGrammarCategoryType.Whitespaces] = whitespaces,
            [LexicalGrammarCategoryType.Keywords] = keywords,
        };
    }
}
