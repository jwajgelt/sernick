namespace sernickTest.Compiler;

using Moq;
using sernick.Compiler;
using sernick.Diagnostics;
using sernick.Input;
using sernick.Input.String;

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

    // NOTE: DO NOT UNSKIP WHEN PUSHING: it will overload Github Actions
    [Theory(Skip = "Too large performance hit on Github Workflow")]
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
    public void CorrectPrograms(string program)
    {
        IInput input = new StringInput(program);
        var diagnostics = new Mock<IDiagnostics>();
        diagnostics.Setup(d => d.DidErrorOccur).Returns(false);

        CompilerFrontend.Process(input, diagnostics.Object);
    }
}
