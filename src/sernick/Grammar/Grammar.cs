namespace sernick.Grammar;
using sernick.Tokenizer.Regex;

using Rules = Dictionary<string, string>;

public class Grammar
{
    // members of grammar, grouped by regions
    // !!! IMPORTANT !!!
    // make sure to only use POSIX-Extended Regular Expressions
    // as specified here: https://en.m.wikibooks.org/wiki/Regular_Expressions/POSIX-Extended_Regular_Expressions
    // Otherwise our StringToRegex might not be able to parse it

    private Rules bracesAndParentheses = new Rules()
    {
        ["leftBrace"] = "{",
        ["rightBrace"] = "}",
        ["leftParentheses"] = "(",
        ["rightParentheses"] = ")",
    };

    private Rules lineDelimiters = new Rules()
    {
        ["semicolon"] = ";"
    };


    private readonly Rules keywords = new Rules()
    {
        ["var"] = "var",
        ["const"] = "const",
        ["function"] = "function",
        ["loop"] = "loop",
        ["break"] = "break",
        ["continue"] = "continue"
    };

    private readonly Rules types = new Rules()
    {
        ["Int"] = "Int",
        ["Bool"] = "Bool",
    };

    private readonly Rules operators = new Rules()
    {
        ["plus"] = "+",
        ["minus"] = "-",
        ["shortCircuitOr"] = "||",
        ["shortCircuitAnd"] = "&&"
    };

    private readonly Rules whitespaces = new Rules()
    {
        ["blankCharacter"] = ":space:"
    };

    private readonly Rules identifiers = new Rules()
    {
        ["typesNames"] = "A-Z[A-Za-z0-9]*",
        ["variableNames"] = "[A-Z|a-z][A-Za-z0-9]*",
    };

    private readonly Rules numbers = new Rules()
    {
        ["integers"] = "[:digit:]+",
    };

    private readonly Rules booleans = new Rules()
    {
        ["true"] = "true",
        ["false"] = "false",
    };

    public List<Regex> generateGrammar()
    {
        var allGrammarRegexes = new List<Dictionary<string, string>>() {
            bracesAndParentheses,
            lineDelimiters,
            keywords,
            types,
            operators,
            whitespaces,
            identifiers,
            numbers,
            booleans
        };

        var listOfListsOfRegexes = allGrammarRegexes.ConvertAll(
            (rulesDictionary) => rulesDictionary.Values.ToList().ConvertAll(
                (regexAsString) => StringToRegex.ToRegex(regexAsString)
            )
        );

        List<Regex> result = listOfListsOfRegexes.SelectMany(x => x).ToList();
        return result;
    }

}
