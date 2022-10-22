namespace sernick.Parser.Ast;

/// <summary>
/// Base class for all types of nodes that can appear in AST (Abstract Syntax Tree)
/// </summary>
public abstract class AstNode {}

public class Identifier
{
	public Identifier(String _name) => name = _name;
	String name;
}

/// <summary>
/// Base class for all types of expressions
/// </summary>
public abstract class Expression : AstNode {}

/// <summary>
/// Class for code blocks (introducing new scope)
/// </summary>
public class CodeBlock : Expression
{
	public CodeBlock(Expression inner_expression) => inner = inner_expression;
	Expression inner;
}

/// <summary>
/// Class representing expressions which consist of many expressions (use of ;)
/// </summary>
public class ExpressionJoin : Expression 
{
	public ExpressionJoin(IEnumerable<Expression> expressions) => inner = expressions;
	IEnumerable<Expression> inner;
}

/// <summary>
/// Base class for all expressions which are created through use of operators
/// </summary>
public abstract class OperatorExp : Expression {}

public class PlusOperator : OperatorExp 
{
	public PlusOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);
	Expression left;
	Expression right;
}

public class MulOperator : OperatorExp
{
	public MulOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);
	Expression left;
	Expression right;
}

public class MinusOperator : OperatorExp 
{
	public MinusOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);
	Expression left;
	Expression right;
}

public class AssignOperator : OperatorExp
{
	public AssignOperator(Identifier _left, Expression _right) => (left, right) = (_left, _right);
	Identifier left;
	Expression right;
}

public class EqualsOperator : OperatorExp
{
	public EqualsOperator(Expression _left, Expression _right) => (left, right) = (_left, _right);
	Expression left;
	Expression right;
}

/// <summary>
/// Base class for types declared eg. in variable declarations
/// </summary>
public abstract class DeclaredType {}

public class BoolType : DeclaredType {}

public class IntType : DeclaredType {}

public class UnitType : DeclaredType {}

public class NoType : DeclaredType {}

/// <summary>
/// Base class for expressions which represent some type of declaration
/// </summary>
public abstract class Declaration : Expression {}

public class ConstDeclaration : Declaration
{
	public ConstDeclaration(Identifier _name, DeclaredType _declaredType, Expression _initValue)
		=> (name, declaredType, initValue) = (_name, _declaredType, _initValue);
	Identifier name;
	DeclaredType declaredType;
	Expression initValue;
}

public class VariableDeclaration : Declaration
{
	public VariableDeclaration(Identifier _name, DeclaredType _declaredType, Expression _initValue)
		=> (name, declaredType, initValue) = (_name, _declaredType, _initValue);
	Identifier name;
	DeclaredType declaredType;
	Expression initValue;
}

public class FunctionDeclaration : Declaration
{
	public FunctionDeclaration(Identifier _name, 
		IEnumerable<ConstDeclaration> _argsDeclaration, 
		DeclaredType _returnType)
		=> (name, argsDeclaration, returnType) = (_name, _argsDeclaration, _returnType);
	Identifier name;
	IEnumerable<ConstDeclaration> argsDeclaration;
	DeclaredType returnType;
}

public class FunctionDefinition : FunctionDeclaration
{
	public FunctionDefinition(Identifier _name, 
		IEnumerable<ConstDeclaration> _argsDeclaration, 
		DeclaredType _returnType,
		CodeBlock _inner) : base(_name, _argsDeclaration, _returnType)
			=> inner = _inner;
	CodeBlock inner;
}

/// <summary>
/// Base class for classes representing flow control statements
/// </summary>
public abstract class FlowControlStatement : Expression {}

public class ContinueStatement : FlowControlStatement {}

public class ReturnStatement : FlowControlStatement 
{
	public ReturnStatement(Expression _returnValue) => returnValue = _returnValue;
	Expression returnValue;
}

public class BreakStatement : FlowControlStatement {}

public class IfStatement : FlowControlStatement 
{
	public IfStatement(Expression _testExpression, CodeBlock _ifBlock, CodeBlock _elseBlock)
		=> (testExpression, ifBlock, elseBlock) = (_testExpression, _ifBlock, _elseBlock);
	Expression testExpression;
	CodeBlock ifBlock;
	CodeBlock elseBlock;
}

public class LoopStatement : FlowControlStatement 
{
	public LoopStatement(CodeBlock innerBlock) => inner = innerBlock;
	CodeBlock inner;
}

/// <summary>
/// Base class for classes representing "simple value" expressions
/// eg. values of variables, literals
/// </summary>
public abstract class SimpleValue : Expression {}

public class ConstValue : SimpleValue
{
	public ConstValue(Identifier _identifier) => identifier = _identifier;
	Identifier identifier;
}

public class VariableValue : SimpleValue
{
	public VariableValue(Identifier _identifier) => identifier = _identifier;
	Identifier identifier;
}

public abstract class LiteralValue : SimpleValue {}

public class BoolLiteralValue : LiteralValue 
{
	public BoolLiteralValue(bool val) => inner = val;
	bool inner;
}

public class IntLiteralValue : LiteralValue 
{
	public IntLiteralValue(int val) => inner = val;
	int inner;
}
public class NoValue : SimpleValue {}