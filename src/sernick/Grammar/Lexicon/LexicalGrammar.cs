namespace sernick.Grammar.Lexicon;

using Common.Regex;
using Tokenizer.Regex;
using CategoryItems = Dictionary<string, string>;

public sealed class LexicalGrammarEntry
{
    public LexicalGrammarCategory Category { get; }
    public Regex<char> Regex { get; }

    private static Regex<char> CreateUnionRegex(CategoryItems categoryItems)
    {
        return string.Join("|", categoryItems.Values).ToRegex();
    }

    public LexicalGrammarEntry(LexicalGrammarCategory category, CategoryItems rules)
    {
        Category = category;
        Regex = CreateUnionRegex(rules);
    }
}

/// <summary>
/// This class defines which combinations of ASCII characters constitute different lexical tokens.
/// </summary>
public static class LexicalGrammar
{
    // !!! IMPORTANT !!!
    // make sure to only use POSIX-Extended Regular Expressions
    // as specified here: https://en.m.wikibooks.org/wiki/Regular_Expressions/POSIX-Extended_Regular_Expressions
    // Otherwise our StringToRegex might not be able to parse it
    //
    // You could use e.g. https://regex101.com to test if the given regex is OK 

    private static readonly LexicalGrammarEntry bracesAndParentheses = new(LexicalGrammarCategory.BracesAndParentheses,
        new CategoryItems
        {
            ["leftBrace"] = "{",
            ["rightBrace"] = "}",
            ["leftParentheses"] = @"\(",
            ["rightParentheses"] = @"\)",
        });

    private static readonly LexicalGrammarEntry semicolon = new(LexicalGrammarCategory.Semicolon, new CategoryItems
    {
        ["semicolon"] = ";"
    });

    private static readonly LexicalGrammarEntry colon = new(LexicalGrammarCategory.Colon, new CategoryItems
    {
        ["colon"] = ":"
    });

    private static readonly LexicalGrammarEntry comma = new(LexicalGrammarCategory.Comma, new CategoryItems
    {
        ["comma"] = ","
    });

    private static readonly LexicalGrammarEntry keywords = new(LexicalGrammarCategory.Keywords, new CategoryItems
    {
        ["var"] = "var",
        ["const"] = "const",
        ["function"] = "fun",
        ["if"] = "if",
        ["else"] = "else",
        ["loop"] = "loop",
        ["break"] = "break",
        ["continue"] = "continue",
        ["return"] = "return",
        ["struct"] = "struct"
    });

    private static readonly LexicalGrammarEntry typeIdentifiers = new(LexicalGrammarCategory.TypeIdentifiers, new CategoryItems
    {
        ["Int"] = "Int",
        ["Bool"] = "Bool",
        ["Unit"] = "Unit",
        ["typeNames"] = "[[:upper:]][[:alnum:]]*",
    });

    private static readonly LexicalGrammarEntry operators = new(LexicalGrammarCategory.Operators, new CategoryItems
    {
        ["plus"] = @"\+",
        ["minus"] = "-",
        ["shortCircuitOr"] = @"\|\|",
        ["shortCircuitAnd"] = "&&",
        ["assignment"] = "=",
        ["equality"] = "==",
        ["notEqual"] = "!=",
        ["greater"] = ">",
        ["less"] = "<",
        ["greaterOrEqual"] = ">=",
        ["lessOrEqual"] = "<=",
        ["star"] = @"\*",
        ["dot"] = @"\."
    });

    private static readonly LexicalGrammarEntry whitespaces = new(LexicalGrammarCategory.Whitespaces, new CategoryItems
    {
        ["blankCharacter"] = "[[:space:]]+"
    });

    private static readonly LexicalGrammarEntry variableIdentifiers = new(LexicalGrammarCategory.VariableIdentifiers, new CategoryItems
    {

        ["variableNames"] = "[[:lower:]][[:alnum:]]*",
    });

    private static readonly LexicalGrammarEntry literals = new(LexicalGrammarCategory.Literals, new CategoryItems
    {
        ["integers"] = "[[:digit:]]+",
        ["true"] = "true",
        ["false"] = "false",
        ["null"] = "null"
    });

    private static readonly LexicalGrammarEntry comments = new(LexicalGrammarCategory.Comments, new CategoryItems
    {
        ["singleLineComment"] = "//.*[[:space:]]*",
        ["multiLineComment"] = @"/\*(.|[[:space:]])*\*/"
    });

    public static Dictionary<LexicalGrammarCategory, LexicalGrammarEntry> GenerateGrammar()
    {
        return new[]
        {
            colon, semicolon, comma, bracesAndParentheses, comments, literals, operators, typeIdentifiers,
            variableIdentifiers, whitespaces, keywords
        }.ToDictionary(entry => entry.Category, entry => entry);
    }
}
