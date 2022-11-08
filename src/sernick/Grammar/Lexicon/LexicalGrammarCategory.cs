namespace sernick.Grammar.Lexicon;

/// <summary>
/// Base class for all grammar categories.
/// </summary>
/// <param name="Priority">
/// 1 -- lowest priority.
/// Keep the priority high for categories which are more general
/// Example: priority of a category which describes "var" keyword
/// should be LOWER than of a category which describes variable identifiers
/// because we want to say that "myvariableName" is a correct variable name,
///and not a mistake in keyword
/// </param>
public abstract record LexicalGrammarCategory(short Priority);

public sealed record BraceCategory() : LexicalGrammarCategory(Priority: 1);

public sealed record LineDelimiterCategory() : LexicalGrammarCategory(Priority: 2);

public sealed record ColonCategory() : LexicalGrammarCategory(Priority: 3);

public sealed record CommaCategory() : LexicalGrammarCategory(Priority: 4);

public sealed record WhitespaceCategory() : LexicalGrammarCategory(Priority: 5);

public sealed record LiteralsCategory() : LexicalGrammarCategory(Priority: 6);

public sealed record OperatorCategory() : LexicalGrammarCategory(Priority: 7);

public sealed record KeywordCategory() : LexicalGrammarCategory(Priority: 8);

public sealed record TypeIdentifierCategory() : LexicalGrammarCategory(Priority: 9);

public sealed record VariableIdentifierCategory() : LexicalGrammarCategory(Priority: 10);

public sealed record CommentCategory() : LexicalGrammarCategory(Priority: 11);
