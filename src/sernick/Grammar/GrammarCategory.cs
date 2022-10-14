namespace sernick.Grammar
{
    public abstract class GrammarCategory
    {
        // 1 -- lowest priority
        public Int16 Priority;

    }

    public class CommentCategory : GrammarCategory
    {
        public new Int16 Priority = 1;
    }

    public class BraceCategory : GrammarCategory
    {
        public new Int16 Priority = 2;
    }

    public class LineDelimiterCategory : GrammarCategory
    {
        public new Int16 Priority = 3;
    }

    public class ColonCategory : GrammarCategory
    {
        public new Int16 Priority = 4;
    }

    public class KeywordCategory : GrammarCategory
    {
        public new Int16 Priority = 8;
    }

    public class TypeIdentifierCategory : GrammarCategory
    {
        public new Int16 Priority = 4;
    }
    public class VariableIdentifierCategory : GrammarCategory
    {
        public new Int16 Priority = 4;
    }

    public class OperatorCategory : GrammarCategory
    {
        public new Int16 Priority = 5;
    }

    public class WhitespaceCategory : GrammarCategory
    {
        public new Int16 Priority = 6;
    }


    public class LiteralsCategory : GrammarCategory
    {
        public new Int16 Priority = 7;
    }


}
