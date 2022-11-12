namespace sernickTest.Compiler;

using Helpers;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Nodes;
using sernick.Diagnostics;
using sernick.Grammar.Lexicon;
using sernick.Grammar.Syntax;
using sernick.Input;
using sernick.Parser;
using sernick.Parser.ParseTree;
using sernick.Tokenizer.Lexer;
using sernick.Utility;

public class FrontendTest
{
    [Theory, MemberData(nameof(CorrectExamplesData))]
    public void TestCorrectExamples(string filePath)
    {
        var diagnostics = filePath.Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    public static IEnumerable<object[]> CorrectExamplesData => Directory
        .GetDirectories("examples", "*", SearchOption.TopDirectoryOnly)
        .Select(dir => new DirectoryInfo($"{dir}/correct")).SelectMany(dirInfo => dirInfo.GetFiles())
        .Select(file => new[] { file.FullName });

    [Theory, MemberData(nameof(IncorrectExamplesData))]
    public void TestIncorrectExamples(string directory, string fileName, IEnumerable<IDiagnosticItem> expectedErrors)
    {
        var diagnostics = $"examples/{directory}/incorrect/{fileName}.ser".Compile();

        Assert.True(diagnostics.DidErrorOccur);
        Assert.Equal(expectedErrors, diagnostics.DiagnosticItems);
    }

    public static IEnumerable<object?[]> IncorrectExamplesData =>
        new[]
        {
            // argument-types
            new object?[] { "argument-types", "call-type-conflict", new[]
            {
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(7, 14) )
            }},
            new object?[] { "argument-types", "no-types", new[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Comma, ","),
                        new Range<ILocation>
                        (
                            FileUtility.LocationAt(2, 19),
                            FileUtility.LocationAt(2, 20)
                        )
                    )
                )
            }},
            new object?[] { "argument-types", "return-value-conflict", new[]
            {
                new TypeCheckingError(new UnitType(), new BoolType(), FileUtility.LocationAt(4, 5) )
            }},
            
            // code-blocks
            new object?[] { "code-blocks", "access_outside_braces", new[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(5, 1), FileUtility.LocationAt(5, 3))),
                    FileUtility.LocationAt(5, 1)
                )
            }},
            new object?[] { "code-blocks", "mixed_brackets", new[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.BracesAndParentheses, ")"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(6, 5),
                        FileUtility.LocationAt(6, 6)
                        )
                    )
                )
            }},
            new object?[] { "code-blocks", "redeclare_after_declaration_in_function_call", new[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(7, 7), FileUtility.LocationAt(7, 8))),
                    FileUtility.LocationAt(7, 7)
                )
            }},
            new object?[] { "code-blocks", "redeclare_outside_parentheses", new[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(5, 7), FileUtility.LocationAt(5, 8))),
                    FileUtility.LocationAt(5, 7)
                )
            }},
            new object?[] { "code-blocks", "unclosed_braces", new[]
            {
                new SyntaxError<Symbol>(null)
            }},
            new object?[] { "code-blocks", "unclosed_parentheses", new[]
            {
                new SyntaxError<Symbol>(null)
            }},
            new object?[] { "code-blocks", "undeclared_after_nested_if", new[]
            {
                new NameResolutionError
                (
                    new Identifier("x2", new Range<ILocation>(FileUtility.LocationAt(7, 21), FileUtility.LocationAt(7, 23))),
                    FileUtility.LocationAt(7, 21)
                )
            }},
            new object?[] { "code-blocks", "usage_before_declaration", new[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(2, 16), FileUtility.LocationAt(2, 17))),
                    FileUtility.LocationAt(7, 21)
                )
            }},
            new object?[] { "code-blocks", "variable_overshadow_in_braces", new[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(4, 9), FileUtility.LocationAt(4, 10))),
                    FileUtility.LocationAt(4, 9)
                )
            }},
            
            // comments-and-separators
            new object?[] { "comments-and-separators", "commented_multi_line_comment", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "comment"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(2, 9),
                        FileUtility.LocationAt(2, 16)
                        )
                    )
                ),
                new LexicalError(FileUtility.LocationAt(2, 17), FileUtility.LocationAt(2, 18)),
                new LexicalError(FileUtility.LocationAt(2, 18), FileUtility.LocationAt(3, 1))
            }},
            new object?[] { "comments-and-separators", "double_end_of_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(4, 1), FileUtility.LocationAt(4, 2)),
                new LexicalError(FileUtility.LocationAt(4, 2), FileUtility.LocationAt(5, 1))
            }},
            new object?[] { "comments-and-separators", "illegal_one_line_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 2)),
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(1, 3),
                        FileUtility.LocationAt(1, 7)
                        )
                    )
                ),
                new LexicalError(FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 3))
            }},
            new object?[] { "comments-and-separators", "illegally_nested_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(5, 1), FileUtility.LocationAt(4, 2)),
                new LexicalError(FileUtility.LocationAt(5, 2), FileUtility.LocationAt(6, 1))
            }},
            new object?[] { "comments-and-separators", "line_without_separator", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>(null)
            }},
            new object?[] { "comments-and-separators", "separarator_in_the_middle", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Semicolon, ";"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(1, 6),
                        FileUtility.LocationAt(1, 7)
                        )
                    )
                )
            }},
            new object?[] { "comments-and-separators", "unclosed_multi_line_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 3)),
                new LexicalError(FileUtility.LocationAt(1, 2), FileUtility.LocationAt(1, 3)),
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(1, 4),
                        FileUtility.LocationAt(1, 8)
                        )
                    )
                )
            }},
            new object?[] { "comments-and-separators", "unopened_multi_line_comment", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(1, 1),
                        FileUtility.LocationAt(1, 5)
                        )
                    )
                ),
                new LexicalError(FileUtility.LocationAt(1, 29), FileUtility.LocationAt(1, 30)),
                new LexicalError(FileUtility.LocationAt(1, 30), FileUtility.LocationAt(2, 1))
            }},
            
            // control_flow
            new object?[] { "control_flow", "else_no_if", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Keywords, "else"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(5, 1),
                        FileUtility.LocationAt(5, 5)
                        )
                    )
                ),
            }},
            new object?[] { "control_flow", "if_else_expression", new IDiagnosticItem[]
            {
                // Is this correct?
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(8, 5))
            }},
            new object?[] { "control_flow", "if_else_expression_unit", new IDiagnosticItem[]
            {
                // Is this correct?
                new TypeCheckingError(new IntType(), new UnitType(), FileUtility.LocationAt(10, 1))
            }},
            new object?[] { "control_flow", "if_syntax", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "condition"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(4, 4),
                        FileUtility.LocationAt(4, 13)
                        )
                    )
                ),
            }},
            
            // default-arguments
            new object?[] { "default-arguments", "non-default-call", new IDiagnosticItem[]
            {
                // not sure what kind of error should there be
                new NameResolutionError
                (
                    new Identifier("nonDefaultCall()", new Range<ILocation>(FileUtility.LocationAt(6, 1), FileUtility.LocationAt(6, 17))),
                    FileUtility.LocationAt(6, 1)
                )
            }},
            new object?[] { "default-arguments", "non-suffix", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.BracesAndParentheses, ")"),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(3, 66),
                        FileUtility.LocationAt(3, 67)
                        )
                    )
                ),
            }},
            new object?[] { "default-arguments", "non-suffix-2", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Comma, ","),
                        new Range<ILocation>
                        (
                        FileUtility.LocationAt(3, 51),
                        FileUtility.LocationAt(3, 52)
                        )
                    )
                ),
            }},
            new object?[] { "default-arguments", "type-conflict", new IDiagnosticItem[]
            {
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(3, 53))
            }},
        };
}
