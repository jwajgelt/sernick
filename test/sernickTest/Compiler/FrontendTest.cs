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

        if (expectedErrors.Any())
        { // some errors are not detected yet
            Assert.True(diagnostics.DidErrorOccur);
        }

        Assert.Equal(expectedErrors, diagnostics.DiagnosticItems);
    }

    public static IEnumerable<object[]> IncorrectExamplesData =>
        new[]
        {
            // argument-types
            new object[] { "argument-types", "call-type-conflict", new IDiagnosticItem[]
            {
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(7, 14))
            }},
            new object[] { "argument-types", "no-types", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Comma, ","),
                        new Range<ILocation>(FileUtility.LocationAt(2, 19), FileUtility.LocationAt(2, 20))
                    )
                )
            }},
            new object[] { "argument-types", "return-value-conflict", new IDiagnosticItem[]
            {
                new TypeCheckingError(new UnitType(), new BoolType(), FileUtility.LocationAt(4, 5))
            }},
            
            // code-blocks
            new object[] { "code-blocks", "access_outside_braces", new IDiagnosticItem[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(5, 1), FileUtility.LocationAt(5, 3))),
                    FileUtility.LocationAt(5, 1)
                )
            }},
            new object[] { "code-blocks", "mixed_brackets", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.BracesAndParentheses, ")"),
                        new Range<ILocation>(FileUtility.LocationAt(6, 5), FileUtility.LocationAt(6, 6))
                    )
                )
            }},
            new object[] { "code-blocks", "redeclare_after_declaration_in_function_call", new IDiagnosticItem[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(7, 7), FileUtility.LocationAt(7, 8))),
                    FileUtility.LocationAt(7, 7)
                )
            }},
            new object[] { "code-blocks", "redeclare_outside_parentheses", new IDiagnosticItem[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(5, 7), FileUtility.LocationAt(5, 8))),
                    FileUtility.LocationAt(5, 7)
                )
            }},
            new object[] { "code-blocks", "unclosed_braces", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>(null)
            }},
            new object[] { "code-blocks", "unclosed_parentheses", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>(null)
            }},
            new object[] { "code-blocks", "undeclared_after_nested_if", new IDiagnosticItem[]
            {
                new NameResolutionError
                (
                    new Identifier("x2", new Range<ILocation>(FileUtility.LocationAt(7, 21), FileUtility.LocationAt(7, 23))),
                    FileUtility.LocationAt(7, 21)
                )
            }},
            new object[] { "code-blocks", "usage_before_declaration", new IDiagnosticItem[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(2, 16), FileUtility.LocationAt(2, 17))),
                    FileUtility.LocationAt(7, 21)
                )
            }},
            new object[] { "code-blocks", "variable_overshadow_in_braces", new IDiagnosticItem[]
            {
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(4, 9), FileUtility.LocationAt(4, 10))),
                    FileUtility.LocationAt(4, 9)
                )
            }},
            
            // comments-and-separators
            new object[] { "comments-and-separators", "commented_multi_line_comment", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "comment"),
                        new Range<ILocation>(FileUtility.LocationAt(2, 9), FileUtility.LocationAt(2, 16))
                    )
                ),
                new LexicalError(FileUtility.LocationAt(2, 17), FileUtility.LocationAt(2, 18)),
                new LexicalError(FileUtility.LocationAt(2, 18), FileUtility.LocationAt(3, 1))
            }},
            new object[] { "comments-and-separators", "double_end_of_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(4, 1), FileUtility.LocationAt(4, 2)),
                new LexicalError(FileUtility.LocationAt(4, 2), FileUtility.LocationAt(5, 1))
            }},
            new object[] { "comments-and-separators", "illegal_one_line_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 2)),
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        new Range<ILocation>(FileUtility.LocationAt(1, 3), FileUtility.LocationAt(1, 7))
                    )
                ),
                new LexicalError(FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 3))
            }},
            new object[] { "comments-and-separators", "illegally_nested_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(5, 1), FileUtility.LocationAt(4, 2)),
                new LexicalError(FileUtility.LocationAt(5, 2), FileUtility.LocationAt(6, 1))
            }},
            new object[] { "comments-and-separators", "line_without_separator", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>(null)
            }},
            new object[] { "comments-and-separators", "separarator_in_the_middle", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Semicolon, ";"),
                        new Range<ILocation>(FileUtility.LocationAt(1, 6), FileUtility.LocationAt(1, 7))
                    )
                )
            }},
            new object[] { "comments-and-separators", "unclosed_multi_line_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 3)),
                new LexicalError(FileUtility.LocationAt(1, 2), FileUtility.LocationAt(1, 3)),
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        new Range<ILocation>(FileUtility.LocationAt(1, 4), FileUtility.LocationAt(1, 8))
                    )
                )
            }},
            new object[] { "comments-and-separators", "unopened_multi_line_comment", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        new Range<ILocation>(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 5))
                    )
                ),
                new LexicalError(FileUtility.LocationAt(1, 29), FileUtility.LocationAt(1, 30)),
                new LexicalError(FileUtility.LocationAt(1, 30), FileUtility.LocationAt(2, 1))
            }},
            
            // control_flow
            new object[] { "control_flow", "else_no_if", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Keywords, "else"),
                        new Range<ILocation>(FileUtility.LocationAt(5, 1), FileUtility.LocationAt(5, 5))
                    )
                ),
            }},
            new object[] { "control_flow", "if_else_expression", new IDiagnosticItem[]
            {
                // Is this correct?
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(8, 5))
            }},
            new object[] { "control_flow", "if_else_expression_unit", new IDiagnosticItem[]
            {
                // Is this correct?
                new TypeCheckingError(new IntType(), new UnitType(), FileUtility.LocationAt(10, 1))
            }},
            new object[] { "control_flow", "if_syntax", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "condition"),
                        new Range<ILocation>(FileUtility.LocationAt(4, 4), FileUtility.LocationAt(4, 13))
                    )
                ),
            }},
            
            // default-arguments
            new object[] { "default-arguments", "non-default-call", new IDiagnosticItem[]
            {
                // not sure what kind of error should there be
                new NameResolutionError
                (
                    new Identifier("nonDefaultCall()", new Range<ILocation>(FileUtility.LocationAt(6, 1), FileUtility.LocationAt(6, 17))),
                    FileUtility.LocationAt(6, 1)
                )
            }},
            new object[] { "default-arguments", "non-suffix", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.BracesAndParentheses, ")"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 66), FileUtility.LocationAt(3, 67))
                    )
                ),
            }},
            new object[] { "default-arguments", "non-suffix-2", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Comma, ","),
                        new Range<ILocation>(FileUtility.LocationAt(3, 51), FileUtility.LocationAt(3, 52))
                    )
                ),
            }},
            new object[] { "default-arguments", "type-conflict", new IDiagnosticItem[]
            {
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(3, 53))
            }},

            //function_naming_readonly_arguments
            new object[] { "function_naming_readonly_arguments", "const_argument_modified", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }},
            new object[] { "function_naming_readonly_arguments", "invalid_character_in_name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Operators, "+"),
                        new Range<ILocation>(FileUtility.LocationAt(2, 13), FileUtility.LocationAt(2, 14))
                    )
                )
            }},
            new object[] { "function_naming_readonly_arguments", "nested_function_modifying_const_global", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }},
            new object[] { "function_naming_readonly_arguments", "nested_function_modifying_const_outer_scoped", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }},
            new object[] { "function_naming_readonly_arguments", "non_const_argument_modified", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }},
            new object[] { "function_naming_readonly_arguments", "pascal_case_function_argument", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "ArgumentName"),
                        new Range<ILocation>(FileUtility.LocationAt(2, 14), FileUtility.LocationAt(2, 26))
                    )
                )
            }},
            new object[] { "function_naming_readonly_arguments", "pascal_case_function_name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "FunctionName"),
                        new Range<ILocation>(FileUtility.LocationAt(2, 5), FileUtility.LocationAt(2, 17))
                    )
                )
            }},

            //loops
            new object[] { "loops", "commented_break", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }},
            new object[] { "loops", "nested_break", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }},
            new object[] { "loops", "nested_no_break", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }},
            new object[] { "loops", "no_break_or_return", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }},
            new object[] { "loops", "uppercase", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "Loop"),
                        new Range<ILocation>(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 5))
                    )
                )
            }},

            //types-and-naming
            new object[] { "types-and-naming", "incorrect-function-argument-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "FirstArgument"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 14), FileUtility.LocationAt(3, 27))
                    )
                )
            }},
            new object[] { "types-and-naming", "incorrect-function-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "InvalidFunctionName"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 5), FileUtility.LocationAt(3, 24))
                    )
                )
            }},
            new object[] { "types-and-naming", "incorrect-type-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "bool"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 12))
                    )
                )
            }},
            new object[] { "types-and-naming", "incorrect-variable-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "InvalidVariableName"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 5), FileUtility.LocationAt(3, 24))
                    )
                )
            }},
            new object[] { "types-and-naming", "nonexistent-type", new IDiagnosticItem[]
            {
                // no error I guess? but we can't define new types
            }},

            //variable-declaration-initialization
            new object[] { "variable-declaration-initialization", "const_bad_type_separator", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "Int"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 9), FileUtility.LocationAt(3, 12))
                    )
                )
            }},
            new object[] { "variable-declaration-initialization", "const_decl_and_init_and_reassignment", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }},
            new object[] { "variable-declaration-initialization", "const_decl_and_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(1, 17))
            }},
            new object[] { "variable-declaration-initialization", "const_decl_bad_type", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Semicolon, ";"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 9))
                    )
                )
            }},
            new object[] { "variable-declaration-initialization", "const_late_init_and_reassignment", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }},
            new object[] { "variable-declaration-initialization", "const_late_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(2, 5))
            }},
            new object[] { "variable-declaration-initialization", "const_redeclaration_grouping", new IDiagnosticItem[]
            {
                // redeclaration
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 9))),
                    FileUtility.LocationAt(3, 8)
                )
            }},
            new object[] { "variable-declaration-initialization", "const_redeclaration", new IDiagnosticItem[]
            {
                // redeclaration
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(3, 7), FileUtility.LocationAt(3, 8))),
                    FileUtility.LocationAt(3, 7)
                )
            }},
            new object[] { "variable-declaration-initialization", "identifier_decl_no_var_or_const_keyword", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Colon, ":"),
                        new Range<ILocation>(FileUtility.LocationAt(5, 2), FileUtility.LocationAt(5, 3))
                    )
                )
            }},
            new object[] { "variable-declaration-initialization", "var_bad_type_separator", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "Int"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 7), FileUtility.LocationAt(3, 10))
                    )
                )
            }},
            new object[] { "variable-declaration-initialization", "var_decl_and_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(1, 15))
            }},
            new object[] { "variable-declaration-initialization", "var_decl_bad_type", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Semicolon, ";"),
                        new Range<ILocation>(FileUtility.LocationAt(3, 6), FileUtility.LocationAt(3, 7))
                    )
                )
            }},
            new object[] { "variable-declaration-initialization", "var_late_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(2, 5))
            }},
            new object[] { "variable-declaration-initialization", "var_redeclaration_grouping", new IDiagnosticItem[]
            {
                // redeclaration
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(3, 6), FileUtility.LocationAt(3, 7))),
                    FileUtility.LocationAt(3, 6)
                )
            }},
            new object[] { "variable-declaration-initialization", "var_redeclaration", new IDiagnosticItem[]
            {
                // redeclaration
                new NameResolutionError
                (
                    new Identifier("x", new Range<ILocation>(FileUtility.LocationAt(3, 5), FileUtility.LocationAt(3, 6))),
                    FileUtility.LocationAt(3, 5)
                )
            }},
        };
}
