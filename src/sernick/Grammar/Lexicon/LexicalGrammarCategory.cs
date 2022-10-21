namespace sernick.Grammar.Lexicon;

public interface ILexicalGrammarCategory
{

    /// <summary> 
    /// 1 -- lowest priority.
    /// Keep the priority high for categories which are more general
    /// Example: priority of a category which describes "var" keyword
    /// should be LOWER than of a category which describes variable identifiers
    /// because we want to say that "myvariableName" is a correct variable name,
    ///and not a mistake in keyword
    /// </summary>
    public short Priority { get; }
}

public sealed class BraceCategory : ILexicalGrammarCategory
{
    public short Priority => 1;
}

public sealed class LineDelimiterCategory : ILexicalGrammarCategory
{
    public short Priority => 2;
}

public sealed class ColonCategory : ILexicalGrammarCategory
{
    public short Priority => 3;
}

public sealed class CommaCategory : ILexicalGrammarCategory
{
    public short Priority => 4;
}

public sealed class WhitespaceCategory : ILexicalGrammarCategory
{
    public short Priority => 5;
}

public sealed class LiteralsCategory : ILexicalGrammarCategory
{
    public short Priority => 6;
}

public sealed class OperatorCategory : ILexicalGrammarCategory
{
    public short Priority => 7;
}
public sealed class KeywordCategory : ILexicalGrammarCategory
{
    public short Priority => 8;
}

public sealed class TypeIdentifierCategory : ILexicalGrammarCategory
{
    public short Priority => 9;
}
public sealed class VariableIdentifierCategory : ILexicalGrammarCategory
{
    public short Priority => 10;
}
public sealed class CommentCategory : ILexicalGrammarCategory
{
    public short Priority => 11;
}
