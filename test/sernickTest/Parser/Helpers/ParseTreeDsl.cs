namespace sernickTest.Parser.Helpers;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Input;
using sernick.Common.Regex;
using sernick.Grammar.Lexicon;
using sernick.Grammar.Syntax;
using sernick.Input;
using sernick.Utility;
using IParseTree = sernick.Parser.ParseTree.IParseTree<sernick.Grammar.Syntax.Symbol>;
using ParseTreeLeaf = sernick.Parser.ParseTree.ParseTreeLeaf<sernick.Grammar.Syntax.Symbol>;
using ParseTreeNode = sernick.Parser.ParseTree.ParseTreeNode<sernick.Grammar.Syntax.Symbol>;

public sealed class ParseTreeDsl : DynamicObject
{
    private sealed record FakeSymbol : Symbol;
    private static readonly Production<Symbol> production = new(new FakeSymbol(), Regex<Symbol>.Empty);
    private static readonly Range<ILocation> locations = new(new FakeLocation(), new FakeLocation());

    public static dynamic PT => new ParseTreeDsl();

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        return TryGetParseTree(binder.Name, out result,
            nonTerminalChildren: () => 
            {
                Debug.Assert(args is not null);
                return args.Select(arg =>
                {
                    Debug.Assert(arg is IParseTree);
                    return (IParseTree)arg;
                });
            },
            terminalSymbolText: () =>
            {
                Debug.Assert(args is not null);
                Debug.Assert(args.Length <= 1);
                var arg = args.Length > 0 ? args[0] : "";
                Debug.Assert(arg is string);
                return (string)arg;
            });
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        return TryGetParseTree(binder.Name, out result,
            nonTerminalChildren: Enumerable.Empty<IParseTree>,
            terminalSymbolText: () => "");
    }

    private static bool TryGetParseTree(
        string symbolName,
        [NotNullWhen(true)] out object? result,
        Func<IEnumerable<IParseTree>> nonTerminalChildren,
        Func<string> terminalSymbolText)
    {
        result = default;

        // Non-terminal
        if (Enum.TryParse(symbolName, out NonTerminalSymbol symbol))
        {
            var children = nonTerminalChildren.Invoke().ToList();
            result = new ParseTreeNode(Symbol.Of(symbol), production, children, locations);
            return true;
        }

        // Terminal
        if (Enum.TryParse(symbolName, out LexicalGrammarCategory category))
        {
            var text = terminalSymbolText.Invoke();
            result = new ParseTreeLeaf(Symbol.Of(category, text), locations);
            return true;
        }

        Debug.Fail($"{symbolName} is neither NonTerminal symbol name nor LexicalGrammar category name");
        return false;
    }
}
