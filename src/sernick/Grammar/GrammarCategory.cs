namespace sernick.Grammar;

public interface IGrammarCategory
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

public class BraceCategory : IGrammarCategory
{
    public short Priority { get; } = 1;
}

public class LineDelimiterCategory : IGrammarCategory
{
    public short Priority { get; } = 2;
}

public class ColonCategory : IGrammarCategory
{
    public short Priority { get; } = 3;
}

public class WhitespaceCategory : IGrammarCategory
{
    public short Priority { get; } = 4;
}

public class LiteralsCategory : IGrammarCategory
{
    public short Priority { get; } = 5;
}

public class OperatorCategory : IGrammarCategory
{
    public short Priority { get; } = 6;
}
public class KeywordCategory : IGrammarCategory
{
    public short Priority { get; } = 7;
}

public class TypeIdentifierCategory : IGrammarCategory
{
    public short Priority { get; } = 8;
}
public class VariableIdentifierCategory : IGrammarCategory
{
    public short Priority { get; } = 9;
}
public class CommentCategory : IGrammarCategory
{
    public short Priority { get; } = 10;
}
