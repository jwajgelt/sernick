namespace sernickTest.Compiler;

public class Frontend
{
    [Fact]
    public void CompileCorrectCode_Case1()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/argument-types/correct/multiple-args.ser");

        Assert.True(diagnostics.Items != null && !diagnostics.Items.Any());
    }

    [Fact]
    public void CompileCorrectCode_Case2()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/code-blocks/correct/code_blocks_return_values.ser");

        Assert.True(diagnostics.Items != null && !diagnostics.Items.Any());
    }

    [Fact]
    public void CompileCorrectCode_Case3()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/control_flow/correct/if_else_expression_true.ser");

        Assert.True(diagnostics.Items != null && !diagnostics.Items.Any());
    }

    [Fact]
    public void CompileCorrectCode_Case4()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/loops/correct/nested.ser");

        Assert.True(diagnostics.Items != null && !diagnostics.Items.Any());
    }

    [Fact]
    public void CompileCorrectCode_Case5()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/variable-declaration-initialization/correct/var_decl_and_init_no_type.ser");

        Assert.True(diagnostics.Items != null && !diagnostics.Items.Any());
    }

    [Fact]
    public void CompileCorrectCode_Case6()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/variable-declaration-initialization/correct/const_decl_no_init.ser");

        Assert.True(diagnostics.Items != null && !diagnostics.Items.Any());
    }

    // '#' and '/' are illegal characters so the lexer shouldn't label them
    [Fact]
    public void CompileIncorrectCode_Case1()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/comments-and-separators/incorrect/illegal_one_line_comment.ser");

        Assert.True(diagnostics.Items != null && diagnostics.Items.Any());
    }

    // '*/' without matching '/*' is illegal so the lexer shouldn't label them
    [Fact(Skip = "Issue with block comment at the moment")]
    public void CompileIncorrectCode_Case2()
    {
        var diagnostics = FrontendTest.Compile("../../../../../examples/comments-and-separators/incorrect/double_end_of_comment.ser");

        Assert.True(diagnostics.Items != null && diagnostics.Items.Any());
    }
}
