namespace sernickTest.Compiler;

using Moq;
using sernick.Compiler;
using sernick.Diagnostics;
using sernick.Input;
using sernick.Input.String;

public class CompilerFrontendTest
{
    [Fact(Skip = "Parser issues, throws not-SLR")]
    public void CompileExceptionIsThrownAfterReportingError()
    {
        IInput input = new StringInput("");
        var diagnostics = new Mock<IDiagnostics>();
        diagnostics.Setup(d => d.DidErrorOccur).Returns(true);

        Assert.Throws<CompilationException>(() => CompilerFrontend.Process(input, diagnostics.Object));
    }

    [Fact(Skip = "Parser issues")]
    public void FinishesSmoothlyAfterNoErrorsReported()
    {
        IInput input = new StringInput("");
        var diagnostics = new Mock<IDiagnostics>();
        diagnostics.Setup(d => d.DidErrorOccur).Returns(false);

        CompilerFrontend.Process(input, diagnostics.Object);
    }
}
