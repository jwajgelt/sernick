namespace sernick.Grammar;
using sernick.Tokenizer.Regex;

using Rules = Dictionary<string, string>;

public class Grammar
{
    // members of grammar, grouped by regions


    private Rules braces = new Rules()
    {
        ["leftCurlyBrace"] = "{",
        ["rightCurlyBrace"] = ""
    };

    private Rules keywords = new Rules()
    {
        ["var"] = "var",
        ["const"] = "const"
    };

    private Rules types = new Rules()
    {
        ["Int"] = "Int",
        ["Bool"] = "Bool",
    };

    private Rules operators = new Rules()
    {
        ["plus"] = "+",
        ["minus"] = "-",
        ["shortCircuitOr"] = "||",
        ["shortCircuitAnd"] = "&&"
    };



    public List<Regex> generateGrammar()
    {
        var allGrammarRegexes = new List<Dictionary<string, string>>() {
            braces,
            keywords,
            types,
            operators
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
