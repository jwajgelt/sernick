namespace sernickTest.Compiler;

using Helpers;
using sernick.Tokenizer.Lexer;

public class FrontendTest
{
    [Fact]
    public void CompileCorrectCode_Case1()
    {
        var diagnostics = "examples/argument-types/correct/multiple-args.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Fact]
    public void CompileCorrectCode_Case2()
    {
        var diagnostics = "examples/code-blocks/correct/code_blocks_return_values.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Fact]
    public void CompileCorrectCode_Case3()
    {
        var diagnostics = "examples/control_flow/correct/if_else_expression_true.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Fact]
    public void CompileCorrectCode_Case4()
    {
        var diagnostics = "examples/loops/correct/nested.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Fact]
    public void CompileCorrectCode_Case5()
    {
        var diagnostics = "examples/variable-declaration-initialization/correct/var_decl_and_init_no_type.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    [Fact]
    public void CompileCorrectCode_Case6()
    {
        var diagnostics = "examples/variable-declaration-initialization/correct/const_decl_no_init.ser".Compile();

        Assert.False(diagnostics.DidErrorOccur);
    }

    // '#' and '/' are illegal characters so the lexer shouldn't label them
    [Fact]
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
