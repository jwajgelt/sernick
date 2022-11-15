namespace sernick.Ast.Nodes.Conversion;

using Grammar.Lexicon;
using Grammar.Syntax;
using Parser.ParseTree;

public static class KeywordsConversion
{
    public static Expression ToKeyword(this IParseTree<Symbol> node) => node switch
    {
        { Symbol: Terminal { Category: LexicalGrammarCategory.Keywords, Text: "break" } }
            => new BreakStatement(node.LocationRange),
        { Symbol: Terminal { Category: LexicalGrammarCategory.Keywords, Text: "continue" } }
            => new ContinueStatement(node.LocationRange),
        _ => throw new ArgumentException("Invalid ParseTree for Keyword Statement")
    };
}
