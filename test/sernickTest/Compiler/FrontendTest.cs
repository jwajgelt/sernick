namespace sernickTest.Compiler;

using Helpers;
using sernick.Tokenizer.Lexer;

public class FrontendTest
{
    [Theory]
    [InlineData("multiple-args")]
    [InlineData("no-args")]
    [InlineData("single-arg")]
    public void CompileCorrectPrograms_ArgumentTypes(string name)
    {
        var diagnostics = $"examples/argument-types/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("access_outside_parentheses")]
    [InlineData("braces_in_function_call")]
    [InlineData("braces_in_if_condition")]
    [InlineData("code_blocks_return_values")]
    [InlineData("parentheses_in_function_call")]
    [InlineData("parentheses_in_if_condition")]
    [InlineData("redeclare_outside_braces")]
    [InlineData("variable_overshadow_in_braces")]
    public void CompileCorrectPrograms_CodeBlocks(string name)
    {
        var diagnostics = $"examples/code-blocks/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("many_commands_in_single_line")]
    [InlineData("multiple_lines_separated")]
    [InlineData("separator_far_away")]
    [InlineData("single_line_separator")]
    [InlineData("spread_command")]
    public void CompileCorrectPrograms_CommentsAndSeparators(string name)
    {
        var diagnostics = $"examples/comments-and-separators/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("if_else_expression_false")]
    [InlineData("if_else_expression_true")]
    [InlineData("if_else_false")]
    [InlineData("if_else_true")]
    [InlineData("if_true")]
    public void CompileCorrectPrograms_ControlFlow(string name)
    {
        var diagnostics = $"examples/control_flow/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("all-args")]
    //[InlineData("arg-suffix")] - mul
    public void CompileCorrectPrograms_DefaultArguments(string name)
    {
        var diagnostics = $"examples/default-arguments/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("camel_case_function_name_and_arguments")]
    //[InlineData("const_argument")] - is this right?
    //[InlineData("names_with_digits")] - same
    //[InlineData("nested_function_declaration_and_call")] - same
    [InlineData("nested_function_modifying_variable_from_outer_scope")]
    [InlineData("recursive_call")]
    public void CompileCorrectPrograms_FunctionNaming(string name)
    {
        var diagnostics = $"examples/function_naming_readonly_arguments/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("break")]
    [InlineData("continue")]
    [InlineData("nested")]
    [InlineData("nested_return")]
    [InlineData("return")]
    [InlineData("simple")]
    public void CompileCorrectPrograms_Loops(string name)
    {
        var diagnostics = $"examples/loops/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("bool")]
    [InlineData("function-arguments")]
    [InlineData("functions")]
    [InlineData("int")]
    [InlineData("variables")]
    public void CompileCorrectPrograms_TypesAndNaming(string name)
    {
        var diagnostics = $"examples/types-and-naming/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("const_decl_and_init_no_type")]
    [InlineData("const_decl_and_init_with_type")]
    [InlineData("const_decl_different_scopes")]
    [InlineData("const_decl_no_init")]
    [InlineData("const_late_init")]
    [InlineData("var_decl_and_init_and_reassignment")]
    [InlineData("var_decl_and_init_no_type")]
    [InlineData("var_decl_and_init_with_type")]
    [InlineData("var_decl_different_scopes")]
    [InlineData("var_decl_no_init")]
    [InlineData("var_late_init")]
    [InlineData("var_late_init_and_reassignment")]
    public void CompileCorrectPrograms_VariableDeclInit(string name)
    {
        var diagnostics = $"examples/variable-declaration-initialization/correct/{name}.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Theory]
    [InlineData("argument-types", "no-types")]
    [InlineData("code-blocks", "mixed_brackets")]
    [InlineData("code-blocks", "unclosed_braces")]
    [InlineData("code-blocks", "unclosed_parentheses")]
    [InlineData("comments-and-separators", "separarator_in_the_middle")]
    [InlineData("control_flow", "else_no_if")]
    [InlineData("default-arguments", "non-suffix-2")]
    [InlineData("default-arguments", "non-suffix")]
    [InlineData("variable-declaration-initialization", "const_bad_type_separator")]
    [InlineData("variable-declaration-initialization", "identifier_decl_no_var_or_const_keyword")]
    public void CompileIncorrectPrograms(string group, string name)
    {
        var diagnostics = $"examples/{group}/incorrect/{name}.ser".Compile();

        Assert.True(diagnostics.DidErrorOccur);
    }

    // '#' and '/' are illegal characters so the lexer shouldn't label them
    [Fact(Skip = "Grammar is not-SLR, causing errors here")]
    public void CompileIncorrectCode_Case1()
    {
        var diagnostics = "examples/comments-and-separators/incorrect/illegal_one_line_comment.ser".Compile();

        Assert.True(diagnostics.DidErrorOccur);
        Assert.Equal(2, diagnostics.DiagnosticItems.Count(item => item is LexicalError));
        Assert.Contains(diagnostics.DiagnosticItems, item =>
        {
            if (item is not LexicalError lexicalError)
            {
                return false;
            }

            return lexicalError.Start.ToString() == "line 1, character 1" && lexicalError.End.ToString() == "line 1, character 2";
        });
        Assert.Contains(diagnostics.DiagnosticItems, item =>
        {
            if (item is not LexicalError lexicalError)
            {
                return false;
            }

            return lexicalError.Start.ToString() == "line 2, character 1" && lexicalError.End.ToString() == "line 2, character 3";
        });
    }

    // '*/' without matching '/*' is illegal so the lexer shouldn't label them
    [Fact(Skip = "Issue with block comment at the moment")]
    public void CompileIncorrectCode_Case2()
    {
        var diagnostics = "examples/comments-and-separators/incorrect/double_end_of_comment.ser".Compile();

        Assert.True(diagnostics.DidErrorOccur);
        Assert.Equal(2, diagnostics.DiagnosticItems.Count(item => item is LexicalError));
        Assert.Contains(diagnostics.DiagnosticItems, item =>
        {
            if (item is not LexicalError lexicalError)
            {
                return false;
            }

            return lexicalError.Start.ToString() == "line 4, character 1" && lexicalError.End.ToString() == "line 4, character 2";
        });
        Assert.Contains(diagnostics.DiagnosticItems, item =>
        {
            if (item is not LexicalError lexicalError)
            {
                return false;
            }

            return lexicalError.Start.ToString() == "line 4, character 2" && lexicalError.End.ToString() == "line 4, character 3";
        });
    }
}
