namespace sernick.Compiler;

public class CompilationException : Exception
{
    public CompilationException(string? message = null) : base(message) { }
}

public class AssemblingException : CompilationException
{
    public AssemblingException(string message) : base(message) { }
}

public class LinkingException : CompilationException
{
    public LinkingException(string message) : base(message) { }
}
