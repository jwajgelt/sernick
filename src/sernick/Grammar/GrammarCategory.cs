namespace sernick.Grammar
{
    public abstract class GrammarCategory
    {
        // 1 -- lowest priority
        // Keep the priority high for categories which are more general
        // Example: priority of a category which describes "var" keyword
        // should be LOWER than of a category which describes variable identifiers
        // because we want to say that "myvariableName" is a correct variable name,
        // and not a mistake in keyword
        public Int16 Priority;

    }

    public class BraceCategory : GrammarCategory
    {
        public new Int16 Priority = 1;
    }

    public class LineDelimiterCategory : GrammarCategory
    {
        public new Int16 Priority = 2;
    }

    public class ColonCategory : GrammarCategory
    {
        public new Int16 Priority = 3;
    }

    public class WhitespaceCategory : GrammarCategory
    {
        public new Int16 Priority = 4;
    }

    public class LiteralsCategory : GrammarCategory
    {
        public new Int16 Priority = 5;
    }

    public class OperatorCategory : GrammarCategory
    {
        public new Int16 Priority = 6;
    }
    public class KeywordCategory : GrammarCategory
    {
        public new Int16 Priority = 7;
    }

    public class TypeIdentifierCategory : GrammarCategory
    {
        public new Int16 Priority = 8;
    }
    public class VariableIdentifierCategory : GrammarCategory
    {
        public new Int16 Priority = 9;
    }
    public class CommentCategory : GrammarCategory
    {
        public new Int16 Priority = 10;
    }
}
