namespace sernickTest.Compiler;

using Helpers;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.TypeChecking;
using sernick.Ast.Nodes;
using sernick.Compiler;
using sernick.Diagnostics;
using sernick.Grammar.Lexicon;
using sernick.Grammar.Syntax;
using sernick.Input;
using sernick.Input.String;
using sernick.Parser;
using sernick.Parser.ParseTree;
using sernick.Tokenizer.Lexer;
using sernick.Utility;
using Utility;

public class CompilerFrontendTest
{
    [Fact]
    public void CompileExceptionIsThrownAfterReportingError()
    {
        IInput input = new StringInput("");
        var diagnostics = new Mock<IDiagnostics>();
        diagnostics.Setup(d => d.DidErrorOccur).Returns(true);

        Assert.Throws<CompilationException>(() => CompilerFrontend.Process(input, diagnostics.Object));
    }

    [Fact]
    public void FinishesSmoothlyAfterNoErrorsReported()
    {
        IInput input = new StringInput("");
        var diagnostics = new Mock<IDiagnostics>();
        diagnostics.Setup(d => d.DidErrorOccur).Returns(false);

        CompilerFrontend.Process(input, diagnostics.Object);
    }

    [Theory, MemberTupleData(nameof(CorrectExamplesData))]
    public void TestCorrectExamples(string group, string fileName)
    {
        var diagnostics = $"examples/{group}/correct/{fileName}.ser".CompileFile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [PerformanceHeavyTheory, MemberTupleData(nameof(IncorrectExamplesData))]
    public void TestIncorrectExamples(string group, string fileName, IEnumerable<IDiagnosticItem> expectedErrors)
    {
        var diagnostics = $"examples/{group}/incorrect/{fileName}.ser".CompileFile();

        if (expectedErrors.Any())
        { // some errors are not detected yet
            Assert.True(diagnostics.DidErrorOccur);
        }

        Assert.Equal(expectedErrors, diagnostics.DiagnosticItems);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("5")]
    [InlineData("{x}")]
    [InlineData("(x)")]
    [InlineData("({x})")]
    [InlineData("{(x)}")]
    [InlineData("a + b")]
    [InlineData("a + {b}")]
    [InlineData("{a} + b")]
    [InlineData("{a} + {b}")]
    [InlineData("{a} {b} + {c}; {d}")]
    [InlineData("{a} {b}")]
    [InlineData("5; {a}")]
    [InlineData("{{a}}")]
    [InlineData("{{a}{b}}")]
    [InlineData("{ a; b; c; }")]
    [InlineData("a == b")]
    [InlineData("{a} == b")]
    [InlineData("{a} == {b}")]
    [InlineData("a == {b}")]
    [InlineData("a + b == c + d")]
    [InlineData("a + {b} == {c} + d")]
    [InlineData("a && b == c && d")]
    [InlineData("{a} && {b} == {c} && {d}")]
    [InlineData("if ({a}) { b } + { c }")]
    [InlineData("if ({a}) { b } else { c } + { d }")]
    [InlineData("if (a) { b } { c }")]
    [InlineData("(a = 1) + 5")]
    [InlineData("{ a = 1; { a = 2 } }")]
    [InlineData("var a: Int = 1; var b: Bool; const c = 2;")]
    [InlineData("a + b() + {c()}")]
    [InlineData("a();")]
    [InlineData("fun f() {} {}")]
    [InlineData("fun f(a: Int, b: Bool = true) { fun g(): Unit {} }")]
    [InlineData("loop { a } { b }")]
    [InlineData("2 + loop { a }")]
    [InlineData("loop { a } + 2")]
    [InlineData("loop { break; continue; return a }")]
    [InlineData("{var x = 1; x} + 5")]
    [InlineData("{var x: Int}")]
    [InlineData("var x: Int = 5 + 5")]
    [InlineData("(var x: Int;);")]
    [InlineData("(fun f() {};);")]
    public void TestCorrectLexerAndParser(string program)
    {
        var diagnostics = program.CompileText();

        Assert.DoesNotContain(
            diagnostics.DiagnosticItems, item =>
            item.GetType() == typeof(LexicalError) ||
            item.GetType() == typeof(SyntaxError<Symbol>));
    }

    public static IEnumerable<(string group, string fileName)> CorrectExamplesData => Directory
        .GetDirectories("examples", "*", SearchOption.TopDirectoryOnly)
        .Select(dir =>
            new DirectoryInfo($"{dir}/correct").GetFiles().Select(
                fileInfo => (group: dir.Split(Path.DirectorySeparatorChar).Last(), fileInfo.Name)))
        .SelectMany(e =>
            e.Select(fileInfo => (fileInfo.group, fileName: fileInfo.Name.Split('.').First())))
        .Where(fileInfo =>
            !fileInfo.Equals(("comments-and-separators", "multi_line_comment")) &&
            !fileInfo.Equals(("comments-and-separators", "multi_line_in_command_comment")) &&
            !fileInfo.Equals(("comments-and-separators", "nested_comment")));

    public static readonly (
        string group,
        string fileName,
        IEnumerable<IDiagnosticItem> expectedErrors
    )[] IncorrectExamplesData = { 
            // argument-types
            ("argument-types", "call-type-conflict", new IDiagnosticItem[]
            {
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(7, 14))
            }),
            ("argument-types", "no-types", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Comma, ","),
                        (FileUtility.LocationAt(2, 19), FileUtility.LocationAt(2, 20))
                    )
                )
            }),
            ("argument-types", "return-value-conflict", new IDiagnosticItem[]
            {
                new TypeCheckingError(new UnitType(), new BoolType(), FileUtility.LocationAt(4, 5))
            }),
            
            // code-blocks
            ("code-blocks", "access_outside_braces", new IDiagnosticItem[]
            {
                new UndeclaredIdentifierError
                (
                    new Identifier("x", (FileUtility.LocationAt(5, 1), FileUtility.LocationAt(5, 2)))
                ),
                new UndeclaredIdentifierError
                (
                    new Identifier("x", (FileUtility.LocationAt(5, 5), FileUtility.LocationAt(5, 6)))
                )
            }),
            ("code-blocks", "mixed_brackets", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.BracesAndParentheses, ")"),
                        (FileUtility.LocationAt(6, 5), FileUtility.LocationAt(6, 6))
                    )
                )
            }),
            ("code-blocks", "redeclare_after_declaration_in_function_call", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(5, 12), FileUtility.LocationAt(5, 13))),
                        new IntType(),
                        new IntLiteralValue(1, (FileUtility.LocationAt(5, 21), FileUtility.LocationAt(5, 22))),
                        true,
                        (FileUtility.LocationAt(5, 6), FileUtility.LocationAt(5, 22))
                    ),
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(7, 7), FileUtility.LocationAt(7, 8))),
                        new IntType(),
                        new IntLiteralValue(2, (FileUtility.LocationAt(7, 16), FileUtility.LocationAt(7, 17))),
                        true,
                        (FileUtility.LocationAt(7, 1), FileUtility.LocationAt(7, 17))
                    )
                )
            }),
            ("code-blocks", "redeclare_outside_parentheses", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(2, 9), FileUtility.LocationAt(2, 10))),
                        new IntType(),
                        new IntLiteralValue(0, (FileUtility.LocationAt(2, 18), FileUtility.LocationAt(2, 19))),
                        false,
                        (FileUtility.LocationAt(2, 5), FileUtility.LocationAt(2, 19))
                    ),
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(5, 7), FileUtility.LocationAt(5, 8))),
                        new IntType(),
                        new IntLiteralValue(1, (FileUtility.LocationAt(5, 16), FileUtility.LocationAt(5, 17))),
                        true,
                        (FileUtility.LocationAt(5, 1), FileUtility.LocationAt(5, 17))
                    )
                )
            }),
            ("code-blocks", "unclosed_braces", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>(null)
            }),
            ("code-blocks", "unclosed_parentheses", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>(null)
            }),
            ("code-blocks", "undeclared_after_nested_if", new IDiagnosticItem[]
            {
                new UndeclaredIdentifierError
                (
                    new Identifier("x2", (FileUtility.LocationAt(7, 21), FileUtility.LocationAt(7, 23)))
                )
            }),
            ("code-blocks", "usage_before_declaration", new IDiagnosticItem[]
            {
                new UndeclaredIdentifierError
                (
                    new Identifier("x", (FileUtility.LocationAt(2, 16), FileUtility.LocationAt(2, 17)))
                )
            }),
            ("code-blocks", "variable_overshadow_in_braces", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(1, 7), FileUtility.LocationAt(1, 8))),
                        new IntType(),
                        new IntLiteralValue(0, (FileUtility.LocationAt(1, 16), FileUtility.LocationAt(1, 17))),
                        true,
                        (FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 17))
                    ),
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(4, 9), FileUtility.LocationAt(4, 10))),
                        new BoolType(),
                        new BoolLiteralValue(false, (FileUtility.LocationAt(4, 19), FileUtility.LocationAt(4, 24))),
                        false,
                        (FileUtility.LocationAt(4, 5), FileUtility.LocationAt(4, 24))
                    )
                )
            }),
            
            // comments-and-separators
            ("comments-and-separators", "commented_multi_line_comment", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "comment"),
                        (FileUtility.LocationAt(2, 9), FileUtility.LocationAt(2, 16))
                    )
                ),
                new LexicalError(FileUtility.LocationAt(2, 17), FileUtility.LocationAt(2, 18)),
                new LexicalError(FileUtility.LocationAt(2, 18), FileUtility.LocationAt(3, 1))
            }),
            // ("comments-and-separators", "double_end_of_comment", new IDiagnosticItem[]
            // {
            //     new LexicalError(FileUtility.LocationAt(4, 1), FileUtility.LocationAt(4, 2)),
            //     new LexicalError(FileUtility.LocationAt(4, 2), FileUtility.LocationAt(5, 1))
            // }),
            ("comments-and-separators", "illegal_one_line_comment", new IDiagnosticItem[]
            {
                new LexicalError(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 2)),
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        (FileUtility.LocationAt(1, 3), FileUtility.LocationAt(1, 7))
                    )
                ),
                new LexicalError(FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 3))
            }),
            // ("comments-and-separators", "illegally_nested_comment", new IDiagnosticItem[]
            // {
            //     new LexicalError(FileUtility.LocationAt(5, 1), FileUtility.LocationAt(4, 2)),
            //     new LexicalError(FileUtility.LocationAt(5, 2), FileUtility.LocationAt(6, 1))
            // }),
            ("comments-and-separators", "line_without_separator", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Keywords, "const"),
                        (FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 6))
                    )
                )
            }),
            ("comments-and-separators", "separarator_in_the_middle", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Semicolon, ";"),
                        (FileUtility.LocationAt(1, 6), FileUtility.LocationAt(1, 7))
                    )
                )
            }),
            // ("comments-and-separators", "unclosed_multi_line_comment", new IDiagnosticItem[]
            // {
            //     new LexicalError(FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 3)),
            //     new LexicalError(FileUtility.LocationAt(1, 2), FileUtility.LocationAt(1, 3)),
            //     new SyntaxError<Symbol>
            //     (
            //         new ParseTreeLeaf<Symbol>
            //         (
            //             new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
            //             (FileUtility.LocationAt(1, 4), FileUtility.LocationAt(1, 8))
            //         )
            //     )
            // }),
            ("comments-and-separators", "unopened_multi_line_comment", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "This"),
                        (FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 5))
                    )
                ),
                new LexicalError(FileUtility.LocationAt(1, 29), FileUtility.LocationAt(1, 30)),
                new LexicalError(FileUtility.LocationAt(1, 30), FileUtility.LocationAt(2, 1))
            }),
            
            // control_flow
            ("control_flow", "else_no_if", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Keywords, "else"),
                        (FileUtility.LocationAt(5, 1), FileUtility.LocationAt(5, 5))
                    )
                ),
            }),
            ("control_flow", "if_else_expression", new IDiagnosticItem[]
            {
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(8, 5))
            }),
            ("control_flow", "if_else_expression_unit", new IDiagnosticItem[]
            {
                new TypeCheckingError(new IntType(), new UnitType(), FileUtility.LocationAt(10, 1))
            }),
            ("control_flow", "if_syntax", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "condition"),
                        (FileUtility.LocationAt(4, 4), FileUtility.LocationAt(4, 13))
                    )
                ),
            }),
            
            // default-arguments
            ("default-arguments", "non-default-call", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new UnitType(), FileUtility.LocationAt(6, 16))
            }),
            ("default-arguments", "non-suffix", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.BracesAndParentheses, ")"),
                        (FileUtility.LocationAt(3, 66), FileUtility.LocationAt(3, 67))
                    )
                ),
            }),
            ("default-arguments", "non-suffix-2", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Comma, ","),
                        (FileUtility.LocationAt(3, 51), FileUtility.LocationAt(3, 52))
                    )
                ),
            }),
            ("default-arguments", "type-conflict", new IDiagnosticItem[]
            {
                new TypeCheckingError(new IntType(), new BoolType(), FileUtility.LocationAt(3, 53))
            }),

            //function_naming_readonly_arguments
            ("function_naming_readonly_arguments", "argument_modified", new IDiagnosticItem[]
            {
                // modifying a parameter, detected by name resolution
                new NotAVariableError(new Identifier("argument", new Range<ILocation>(FileUtility.LocationAt(3, 2), FileUtility.LocationAt(3, 10))))
            }),
            ("function_naming_readonly_arguments", "const_argument", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Keywords, "const"),
                        (FileUtility.LocationAt(2, 14), FileUtility.LocationAt(2, 19))
                    )
                )
            }),
            ("function_naming_readonly_arguments", "invalid_character_in_name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Operators, "+"),
                        (FileUtility.LocationAt(2, 13), FileUtility.LocationAt(2, 14))
                    )
                )
            }),
            ("function_naming_readonly_arguments", "nested_function_modifying_const_global", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }),
            ("function_naming_readonly_arguments", "nested_function_modifying_const_outer_scoped", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }),
            ("function_naming_readonly_arguments", "pascal_case_function_argument", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "ArgumentName"),
                        (FileUtility.LocationAt(2, 14), FileUtility.LocationAt(2, 26))
                    )
                )
            }),
            ("function_naming_readonly_arguments", "pascal_case_function_name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "FunctionName"),
                        (FileUtility.LocationAt(2, 5), FileUtility.LocationAt(2, 17))
                    )
                )
            }),

            //loops
            ("loops", "commented_break", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }),
            ("loops", "nested_break", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }),
            ("loops", "nested_no_break", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }),
            ("loops", "no_break_or_return", new IDiagnosticItem[]
            {
                // no break in loop, not detected yet
            }),
            ("loops", "uppercase", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "Loop"),
                        (FileUtility.LocationAt(1, 1), FileUtility.LocationAt(1, 5))
                    )
                )
            }),

            //types-and-naming
            ("types-and-naming", "incorrect-function-argument-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "FirstArgument"),
                        (FileUtility.LocationAt(3, 14), FileUtility.LocationAt(3, 27))
                    )
                )
            }),
            ("types-and-naming", "incorrect-function-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "InvalidFunctionName"),
                        (FileUtility.LocationAt(3, 5), FileUtility.LocationAt(3, 24))
                    )
                )
            }),
            ("types-and-naming", "incorrect-type-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "bool"),
                        (FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 12))
                    )
                )
            }),
            ("types-and-naming", "incorrect-variable-name", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "InvalidVariableName"),
                        (FileUtility.LocationAt(3, 5), FileUtility.LocationAt(3, 24))
                    )
                )
            }),
            ("types-and-naming", "nonexistent-type", new IDiagnosticItem[]
            {
                // nonexistent type, error not detected yet
            }),

            //variable-declaration-initialization
            ("variable-declaration-initialization", "const_bad_type_separator", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "Int"),
                        (FileUtility.LocationAt(3, 9), FileUtility.LocationAt(3, 12))
                    )
                )
            }),
            ("variable-declaration-initialization", "const_decl_and_init_and_reassignment", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }),
            ("variable-declaration-initialization", "const_decl_and_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(1, 17))
            }),
            ("variable-declaration-initialization", "const_decl_bad_type", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Semicolon, ";"),
                        (FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 9))
                    )
                )
            }),
            ("variable-declaration-initialization", "const_late_init_and_reassignment", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }),
            ("variable-declaration-initialization", "const_late_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(2, 5))
            }),
            ("variable-declaration-initialization", "const_redeclaration_grouping", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(2, 7), FileUtility.LocationAt(2, 8))),
                        new IntType(), null, true, (FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 13))
                    ),
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 9))),
                        new IntType(), null, true, (FileUtility.LocationAt(3, 2), FileUtility.LocationAt(3, 14))
                    )
                )
            }),
            ("variable-declaration-initialization", "const_redeclaration", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(2, 7), FileUtility.LocationAt(2, 8))),
                        new IntType(), null, true, (FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 13))
                    ),
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(3, 7), FileUtility.LocationAt(3, 8))),
                        new IntType(), null, true, (FileUtility.LocationAt(3, 1), FileUtility.LocationAt(3, 13))
                    )
                )
            }),
            ("variable-declaration-initialization", "identifier_decl_no_var_or_const_keyword", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Colon, ":"),
                        (FileUtility.LocationAt(5, 2), FileUtility.LocationAt(5, 3))
                    )
                )
            }),
            ("variable-declaration-initialization", "var_bad_type_separator", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.TypeIdentifiers, "Int"),
                        (FileUtility.LocationAt(3, 7), FileUtility.LocationAt(3, 10))
                    )
                )
            }),
            ("variable-declaration-initialization", "var_decl_and_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(1, 15))
            }),
            ("variable-declaration-initialization", "var_decl_bad_type", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Semicolon, ";"),
                        (FileUtility.LocationAt(3, 6), FileUtility.LocationAt(3, 7))
                    )
                )
            }),
            ("variable-declaration-initialization", "var_late_init_bad_type", new IDiagnosticItem[]
            {
                new TypeCheckingError(new BoolType(), new IntType(), FileUtility.LocationAt(2, 5))
            }),
            ("variable-declaration-initialization", "var_redeclaration_grouping", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(2, 5), FileUtility.LocationAt(2, 6))),
                        new IntType(), null, false, (FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 11))
                    ),
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(3, 6), FileUtility.LocationAt(3, 7))),
                        new IntType(), null, false, (FileUtility.LocationAt(3, 2), FileUtility.LocationAt(3, 12))
                    )
                )
            }),
            ("variable-declaration-initialization", "var_redeclaration", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(2, 5), FileUtility.LocationAt(2, 6))),
                        new IntType(), null, false, (FileUtility.LocationAt(2, 1), FileUtility.LocationAt(2, 11))
                    ),
                    new VariableDeclaration(
                        new Identifier("x", (FileUtility.LocationAt(3, 5), FileUtility.LocationAt(3, 6))),
                        new IntType(), null, false, (FileUtility.LocationAt(3, 1), FileUtility.LocationAt(3, 11))
                    )
                )
            }),
        };
}
