namespace sernick.Grammar.Lexicon;

/// <summary>
/// 1 -- lowest priority.
/// Keep the priority high for categories which are more general
/// Example: priority of a category which describes "var" keyword
/// should be LOWER than of a category which describes variable identifiers
/// because we want to say that "myvariableName" is a correct variable name,
///and not a mistake in keyword
/// </summary>
public enum LexicalGrammarCategory : short
{
    BracesAndParentheses = 1,
    Semicolon = 2,
    Colon = 3,
    Comma = 4,
    Whitespaces = 5,
    Literals = 6,
    Operators = 7,
    Keywords = 8,
    TypeIdentifiers = 9,
    VariableIdentifiers = 10,
    Comments = 11
}
