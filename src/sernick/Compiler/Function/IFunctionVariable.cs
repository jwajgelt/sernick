namespace sernick.Compiler.Function;

/// <summary>
///     Empty interface used for marking AST nodes which represent Variables.
///     This is to make Backend implementation not depend on AST classes from frontend.
/// </summary>
public interface IFunctionVariable { }

public interface IFunctionParam : IFunctionVariable { }
