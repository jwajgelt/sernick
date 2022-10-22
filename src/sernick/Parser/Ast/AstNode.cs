namespace sernick.Parser.Ast;

/// <summary>
/// Base class for all types of nodes that can appear in AST (Abstract Syntax Tree)
/// </summary>
public abstract class AstNode { }

public class Identifier
{
    public Identifier(string _name) => name = _name;

    private string name { get; }
}

/// <summary>
/// Base class for all types of expressions
/// </summary>
public abstract class Expression : AstNode { }

/// <summary>
/// Class for code blocks (introducing new scope)
/// </summary>
public class CodeBlock : Expression
{
    public CodeBlock(Expression inner_expression) => inner = inner_expression;

    private Expression inner { get; }
}

/// <summary>
/// Class representing expressions which consist of many expressions (use of ;)
/// </summary>
public class ExpressionJoin : Expression
{
    public ExpressionJoin(IEnumerable<Expression> expressions) => inner = expressions;

    private IEnumerable<Expression> inner { get; }
}

/// <summary>
/// Base class for all expressions which are created through use of operators
/// </summary>
public abstract class OperatorExp : Expression { }

public class PlusOperator : OperatorExp
{
    public PlusOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);

    private Expression left { get; }
    private Expression right { get; }
}

public class MulOperator : OperatorExp
{
    public MulOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);

    private Expression left { get; }
    private Expression right { get; }
}

public class MinusOperator : OperatorExp
{
    public MinusOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);

    private Expression left { get; }
    private Expression right { get; }
}

public class AssignOperator : OperatorExp
{
    public AssignOperator(Identifier _left, Expression _right) => (left, right) = (_left, _right);

    private Identifier left { get; }
    private Expression right { get; }
}

public class EqualsOperator : OperatorExp
{
    public EqualsOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);

    private Expression left { get; }
    private Expression right { get; }
}

/// <summary>
/// Base class for types declared eg. in variable declarations
/// </summary>
public abstract class DeclaredType { }

public class BoolType : DeclaredType { }

public class IntType : DeclaredType { }

public class UnitType : DeclaredType { }

public class NoType : DeclaredType { }

/// <summary>
/// Base class for expressions which represent some type of declaration
/// </summary>
public abstract class Declaration : Expression { }

public class ConstDeclaration : Declaration
{
    public ConstDeclaration(Identifier _name, DeclaredType _declaredType, Expression _initValue)
        => (name, declaredType, initValue) = (_name, _declaredType, _initValue);

    private Identifier name { get; }
    private DeclaredType declaredType { get; }
    private Expression initValue { get; }
}

public class VariableDeclaration : Declaration
{
    public VariableDeclaration(Identifier _name, DeclaredType _declaredType, Expression _initValue)
        => (name, declaredType, initValue) = (_name, _declaredType, _initValue);

    private Identifier name { get; }
    private DeclaredType declaredType { get; }
    private Expression initValue { get; }
}

public class FunctionDeclaration : Declaration
{
    public FunctionDeclaration(Identifier _name,
        IEnumerable<ConstDeclaration> _argsDeclaration,
        DeclaredType _returnType)
        => (name, argsDeclaration, returnType) = (_name, _argsDeclaration, _returnType);

    private Identifier name { get; }
    private IEnumerable<ConstDeclaration> argsDeclaration { get; }
    private DeclaredType returnType { get; }
}

public class FunctionDefinition : FunctionDeclaration
{
    public FunctionDefinition(Identifier _name,
        IEnumerable<ConstDeclaration> _argsDeclaration,
        DeclaredType _returnType,
        CodeBlock _inner) : base(_name, _argsDeclaration, _returnType)
            => inner = _inner;

    private CodeBlock inner { get; }
}

/// <summary>
/// Base class for classes representing flow control statements
/// </summary>
public abstract class FlowControlStatement : Expression { }

public class ContinueStatement : FlowControlStatement { }

public class ReturnStatement : FlowControlStatement
{
    public ReturnStatement(Expression _returnValue) => returnValue = _returnValue;

    private Expression returnValue { get; }
}

public class BreakStatement : FlowControlStatement { }

public class IfStatement : FlowControlStatement
{
    public IfStatement(Expression _testExpression, CodeBlock _ifBlock, CodeBlock _elseBlock)
        => (testExpression, ifBlock, elseBlock) = (_testExpression, _ifBlock, _elseBlock);

    private Expression testExpression { get; }
    private CodeBlock ifBlock { get; }
    private CodeBlock elseBlock { get; }
}

public class LoopStatement : FlowControlStatement
{
    public LoopStatement(CodeBlock innerBlock) => inner = innerBlock;

    private CodeBlock inner { get; }
}

/// <summary>
/// Base class for classes representing "simple value" expressions
/// eg. values of variables, literals
/// </summary>
public abstract class SimpleValue : Expression { }

public class ConstValue : SimpleValue
{
    public ConstValue(Identifier _identifier) => identifier = _identifier;

    private Identifier identifier { get; }
}

public class VariableValue : SimpleValue
{
    public VariableValue(Identifier _identifier) => identifier = _identifier;

    private Identifier identifier { get; }
}

public abstract class LiteralValue : SimpleValue { }

public class BoolLiteralValue : LiteralValue
{
    public BoolLiteralValue(bool val) => inner = val;

    private bool inner { get; }
}

public class IntLiteralValue : LiteralValue
{
    public IntLiteralValue(int val) => inner = val;

    private int inner { get; }
}
public class NoValue : SimpleValue { }
