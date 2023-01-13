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

    [Theory]
    [MemberTupleData(nameof(CorrectExamplesData))]
    public void TestCorrectExamples(string group, string fileName)
    {
        var diagnostics = $"examples/{group}/correct/{fileName}.ser".CompileFile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [MemberTupleData(nameof(IncorrectExamplesData))]
    public void TestIncorrectExamples(string group, string fileName, IDiagnosticItem[] expectedErrors)
    {
        var diagnostics = $"examples/{group}/incorrect/{fileName}.ser".CompileFile();

        if (expectedErrors.Any())
        { // some errors are not detected yet
            Assert.True(diagnostics.DidErrorOccur);
        }

        Assert.Equal(expectedErrors, diagnostics.DiagnosticItems);
    }

    [Theory]
    [InlineData(@"//    0")]
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
    [InlineData("struct Struct { a: Int, b : Bool }")]
    [InlineData("var x = Struct { a: 0, b: false }")]
    [InlineData("var x: **Struct = null")]
    [InlineData("*(**y + 2)")]
    [InlineData("*(*x.field + 2).field")]
    public void TestCorrectLexerAndParser(string program)
    {
        var diagnostics = program.CompileText();

        Assert.DoesNotContain(
            diagnostics.DiagnosticItems, item =>
            item is LexicalError or SyntaxError<Symbol>);
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
        IDiagnosticItem[] expectedErrors
    )[] IncorrectExamplesData = { 
            // argument-types
            ("argument-types", "call-type-conflict", new IDiagnosticItem[]
            {
                new WrongFunctionArgumentError(new IntType(), new BoolType(), FileUtility.LocationAt(7, 14))
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
                new InferredBadFunctionReturnType(new UnitType(), new BoolType(), FileUtility.LocationAt(4, 2))
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
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "is"),
                        (FileUtility.LocationAt(1, 8), FileUtility.LocationAt(1, 10))
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
                        new Terminal(LexicalGrammarCategory.VariableIdentifiers, "comment"),
                        (FileUtility.LocationAt(1, 6), FileUtility.LocationAt(1, 13))
                    )
                ),
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
                new UnequalBranchTypeError(new IntType(), new BoolType(), FileUtility.LocationAt(5, 11))
            }),
            ("control_flow", "if_else_expression_unit", new IDiagnosticItem[]
            {
                new UnequalBranchTypeError(new IntType(), new UnitType(), FileUtility.LocationAt(6, 11))
            }),
            ("control_flow", "if_syntax_body", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Keywords, "var"),
                        (FileUtility.LocationAt(5, 16), FileUtility.LocationAt(5, 19))
                    )
                ),
            }),
            ("control_flow", "if_syntax_condition", new IDiagnosticItem[]
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
                new WrongNumberOfFunctionArgumentsError(1, 0, FileUtility.LocationAt(6, 1))
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
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(3, 53))
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
                // Since `Loop` is uppercase it is recognized as a struct-value expression.
                // Break keyword is illegal in a struct-value expression
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Keywords, "break"),
                        (FileUtility.LocationAt(2, 5), FileUtility.LocationAt(2, 10))
                    )
                )
            }),
            
            // pointers
            ("pointers", "assign_ptr_to_type", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new IntType(),
                    new PointerType(new IntType()),
                    FileUtility.LocationAt(6, 9)
                )
            }),
            ("pointers", "assign_to_const_pointer", new IDiagnosticItem[]
            {
                // modifying const, not detected yet
            }),
            ("pointers", "assign_two_nulls", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new PointerType(new BoolType()),
                    new PointerType(new IntType()),
                    FileUtility.LocationAt(4, 22)
                )
            }),
            ("pointers", "assign_type_to_ptr", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new PointerType(new IntType()),
                    new IntType(),
                    FileUtility.LocationAt(6, 10)
                )
            }),
            ("pointers", "different_pointer_types_assign", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new PointerType(new IntType()),
                    new PointerType(new BoolType()),
                    FileUtility.LocationAt(6, 10)
                )
            }),
            ("pointers", "different_pointer_types_values_assign", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new IntType(),
                    new BoolType(),
                    FileUtility.LocationAt(6, 11)
                )
            }),
            ("pointers", "null_to_type", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new IntType(),
                    new NullPointerType(),
                    FileUtility.LocationAt(4, 10)
                )
            }),
            ("pointers", "typeless_null", new IDiagnosticItem[]
            {
               new TypeOrInitialValueShouldBePresentError(FileUtility.LocationAt(3, 1))
            }),
            ("pointers", "write_bad_value", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new IntType(),
                    new BoolType(),
                    FileUtility.LocationAt(4, 11)
                )
            }),
            ("pointers", "write_to_bad_type", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new BoolType(),
                    new IntType(),
                    FileUtility.LocationAt(4, 23)
                )
            }),
            
            // structs
            ("structs", "assign_overshadowed_type", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new StructType( new Identifier("TestStruct", FileUtility.Line(7).Range(17, 27))),
                    new StructType( new Identifier("TestStruct", FileUtility.Line(15).Range(18, 28))),
                    FileUtility.LocationAt(15, 18)
                )
            }),
            // TODO: Implement const analysis for struct
            // ("structs", "assign_to_const_field", new IDiagnosticItem[]
            // {
            //     // modifying const, not detected yet
            // }),
            // ("structs", "assign_to_const_struct", new IDiagnosticItem[]
            // {
            //     // modifying const, not detected yet
            // }),
            // ("structs", "assign_to_nested_const_struct", new IDiagnosticItem[]
            // {
            //     // modifying const, not detected yet
            // }),
            ("structs", "bad_expression_type_in_initialization", new IDiagnosticItem[]
            {
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(8, 13))
            }),
            ("structs", "bad_field_name_initialization", new IDiagnosticItem[]
            {
                new FieldNotPresentInStructError(
                    new StructType(new Identifier("TestStruct", (FileUtility.LocationAt(7, 18), FileUtility.LocationAt(7, 28)))),
                    new Identifier("differentFieldName", (FileUtility.LocationAt(8, 5), FileUtility.LocationAt(8, 23))),
                    FileUtility.LocationAt(8, 5)
                    ),
                new MissingFieldInitialization(
                    new StructType(new Identifier("TestStruct", (FileUtility.LocationAt(7, 18), FileUtility.LocationAt(7, 28)))),
                    "field",
                    FileUtility.LocationAt(9, 2)
                    )
            }),
            ("structs", "bad_struct_name_initialization", new IDiagnosticItem[]
            {
                new NotATypeError
                (
                    new Identifier("TestStructButWithSomeWeirdStuff", (FileUtility.LocationAt(7, 18), FileUtility.LocationAt(7, 49)))
                )
            }),
            ("structs", "bad_type_initialization", new IDiagnosticItem[]
            {
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(8, 12))
            }),
            ("structs", "field_without_type", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Comma, ","),
                        (FileUtility.LocationAt(4, 11), FileUtility.LocationAt(4, 12))
                    )
                )
            }),
            ("structs", "grouping_struct_redeclaration", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new StructDeclaration
                    (
                        new Identifier("TestStruct", (FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 18))),
                        new []
                        {
                            new FieldDeclaration
                            (
                                new Identifier("field", (FileUtility.LocationAt(4, 5), FileUtility.LocationAt(4, 10))),
                                new IntType(),
                                (FileUtility.LocationAt(4, 5), FileUtility.LocationAt(4, 15))
                            )
                        },
                        (FileUtility.LocationAt(3, 1), FileUtility.LocationAt(5, 2))
                    ),
                    new StructDeclaration
                    (
                        new Identifier("TestStruct", (FileUtility.LocationAt(8, 12), FileUtility.LocationAt(8, 22))),
                        new []
                        {
                            new FieldDeclaration
                            (
                                new Identifier("field", (FileUtility.LocationAt(9, 9), FileUtility.LocationAt(9, 14))),
                                new BoolType(),
                                (FileUtility.LocationAt(9, 9), FileUtility.LocationAt(9, 20))
                            )
                        },
                        (FileUtility.LocationAt(8, 5), FileUtility.LocationAt(10, 6))
                    )
                )
            }),
            ("structs", "incorrect_brackets_in_declaration", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.BracesAndParentheses, "("),
                        (FileUtility.LocationAt(3, 19), FileUtility.LocationAt(3, 20))
                    )
                )
            }),
            ("structs", "incorrect_struct_set", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new StructType( new Identifier("TestStruct1", FileUtility.Line(11).Range(19, 30))),
                    new StructType( new Identifier("TestStruct2", FileUtility.Line(15).Range(15, 26))),
                    FileUtility.LocationAt(15, 15)
                )
            }),
            ("structs", "incorrect_type_set", new IDiagnosticItem[]
            {
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(17, 21)),
                new TypesMismatchError(new BoolType(), new IntType(), FileUtility.LocationAt(18, 21)),
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(19, 21)),
                new TypesMismatchError
                (
                    new IntType(),
                    new StructType( new Identifier("TestStruct", FileUtility.Line(10).Range(18, 28))),
                    FileUtility.LocationAt(20, 21)
                )
            }),
            ("structs", "incorrect_type_usage", new IDiagnosticItem[]
            {
                new TypesMismatchError
                (
                    new StructType( new Identifier("AnotherStruct", FileUtility.Line(23).Range(12, 25))),
                    new StructType( new Identifier("InnerStruct", FileUtility.Line(12).Range(12, 23))),
                    FileUtility.LocationAt(23, 28)
                )
            }),
            ("structs", "initialize_additional_field", new IDiagnosticItem[]
            {
                new FieldNotPresentInStructError
                (
                    new StructType(
                        new Identifier("TestStruct", FileUtility.Line(7).Range(18, 28))
                        ),
                    new Identifier("additionalField", FileUtility.Line(9).Range(5, 20)),
                    FileUtility.LocationAt(9, 5)
                )
            }),
            ("structs", "initialize_not_all_fields", new IDiagnosticItem[]
            {
                new MissingFieldInitialization(
                    new StructType(new Identifier("TestStruct", FileUtility.Line(10).Range(18, 28))),
                    "field4",
                    FileUtility.LocationAt(14, 2))
            }),
            ("structs", "nonexistent_field_read", new IDiagnosticItem[]
            {
                new FieldNotPresentInStructError
                (
                    new StructType( new Identifier("TestStruct", FileUtility.Line(7).Range(20, 30))),
                    new Identifier("anotherField", FileUtility.Line(11).Range(24, 36)),
                    FileUtility.LocationAt(11, 24)
                )
            }),
            ("structs", "nonexistent_field_write", new IDiagnosticItem[]
            {
                new FieldNotPresentInStructError
                (
                    new StructType( new Identifier("TestStruct", FileUtility.Line(7).Range(20, 30))),
                    new Identifier("anotherField", FileUtility.Line(11).Range(12, 24)),
                    FileUtility.LocationAt(11, 12)
                )
            }),
            ("structs", "nonexistent_struct_initialization", new IDiagnosticItem[]
            {
                new NotATypeError
                (
                    new Identifier("TestStruct", (FileUtility.LocationAt(3, 18), FileUtility.LocationAt(3, 28)))
                )
            }),
            ("structs", "nonexistent_type_in_declaration", new IDiagnosticItem[]
            {
                new NotATypeError
                (
                    new Identifier("NonExistentType", (FileUtility.LocationAt(4, 12), FileUtility.LocationAt(4, 27)))
                )
            }),
            ("structs", "overshadowed_struct_in_nested_scope", new IDiagnosticItem[]
            {
                new FieldNotPresentInStructError
                (
                    new StructType(new Identifier("TestStruct", FileUtility.Line(19).Range(23, 33))),
                    new Identifier("otherField", FileUtility.Line(20).Range(5, 15)),
                    FileUtility.LocationAt(20, 5)
                ),
                new MissingFieldInitialization(
                    new StructType(new Identifier("TestStruct", FileUtility.Line(19).Range(23, 33))),
                    "field",
                    FileUtility.LocationAt(21, 2)
                    )
            }),
            ("structs", "recursive_type", new IDiagnosticItem[]
            {
                new RecursiveStructDeclaration(
                    new FieldDeclaration(
                        Name: new Identifier("another", FileUtility.Line(7).Range(5, 12)),
                        Type: new StructType(new Identifier("RecStruct", FileUtility.Line(7).Range(14, 23))),
                        LocationRange: FileUtility.Line(7).Range(5, 23)
                        )
                )
            }),
            ("structs", "repeat_fields_in_initialization", new IDiagnosticItem[]
            {
                new DuplicateFieldInitialization("field1",
                    First: FileUtility.Line(11).Range(5, 14),
                    Second: FileUtility.Line(12).Range(5, 14))
            }),
            ("structs", "struct_in_different_scope", new IDiagnosticItem[]
            {
                new NotATypeError
                (
                    new Identifier("TestStruct", (FileUtility.LocationAt(9, 18), FileUtility.LocationAt(9, 28)))
                )
            }),
            ("structs", "struct_redeclaration", new IDiagnosticItem[]
            {
                new MultipleDeclarationsError
                (
                    new StructDeclaration
                    (
                        new Identifier("TestStruct", (FileUtility.LocationAt(3, 8), FileUtility.LocationAt(3, 18))),
                        new []
                        {
                            new FieldDeclaration
                            (
                                new Identifier("field", (FileUtility.LocationAt(4, 5), FileUtility.LocationAt(4, 10))),
                                new IntType(),
                                (FileUtility.LocationAt(4, 5), FileUtility.LocationAt(4, 15))
                            )
                        },
                        (FileUtility.LocationAt(3, 1), FileUtility.LocationAt(5, 2))
                    ),
                    new StructDeclaration
                    (
                        new Identifier("TestStruct", (FileUtility.LocationAt(7, 8), FileUtility.LocationAt(7, 18))),
                        new []
                        {
                            new FieldDeclaration
                            (
                                new Identifier("field", (FileUtility.LocationAt(8, 5), FileUtility.LocationAt(8, 10))),
                                new BoolType(),
                                (FileUtility.LocationAt(8, 5), FileUtility.LocationAt(4, 16))
                            )
                        },
                        (FileUtility.LocationAt(7, 1), FileUtility.LocationAt(9, 2))
                    )
                )
            }),
            ("structs", "value_in_declaration", new IDiagnosticItem[]
            {
                new SyntaxError<Symbol>
                (
                    new ParseTreeLeaf<Symbol>
                    (
                        new Terminal(LexicalGrammarCategory.Operators, "="),
                        (FileUtility.LocationAt(4, 16), FileUtility.LocationAt(4, 17))
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
                new NotATypeError(
                    new Identifier("String", FileUtility.Line(3).Range(8, 14)))
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
                new TypesMismatchError(new BoolType(), new IntType(), FileUtility.LocationAt(1, 17)),
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(2, 16))
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
                new TypesMismatchError(new BoolType(), new IntType(), FileUtility.LocationAt(2, 5)),
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(5, 5)),
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
                new TypesMismatchError(new BoolType(), new IntType(), FileUtility.LocationAt(1, 15)),
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(2, 14)),
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
                new TypesMismatchError(new BoolType(), new IntType(), FileUtility.LocationAt(2, 5)),
                new TypesMismatchError(new IntType(), new BoolType(), FileUtility.LocationAt(5, 5))
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
