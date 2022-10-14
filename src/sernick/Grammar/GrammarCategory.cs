namespace sernick.Grammar;

public interface IGrammarCategory
{
    // 1 -- lowest priority
    // Keep the priority high for categories which are more general
    // Example: priority of a category which describes "var" keyword
    // should be LOWER than of a category which describes variable identifiers
    // because we want to say that "myvariableName" is a correct variable name,
    // and not a mistake in keyword
    public Int16 Priority { get; }

}

public class BraceCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 1;
}


public class LineDelimiterCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 2;
}

public class ColonCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 3;
}

public class WhitespaceCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 4;
}

public class LiteralsCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 5;
}

public class OperatorCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 6;
}
public class KeywordCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 7;
}

public class TypeIdentifierCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 8;
}
public class VariableIdentifierCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 9;
}
public class CommentCategory : IGrammarCategory
{
    public Int16 Priority { get; } = 10;
}