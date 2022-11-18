namespace sernickTest.Ast.Helpers;

using Input;
using sernick.Ast;
using sernick.Ast.Nodes;
using sernick.Input;
using sernick.Utility;

/// <summary>
/// Import this class statically (<i>using static</i>) to take advantage of a more readable AST construction API.
/// This class doesn't cover all possible usecases, so if you lack some method, just add it here so that it conforms
/// to the general style of the API (either static method or extension method; 2 overloads both with <i>out var</i> and without)
/// </summary>
public static class AstNodesExtensions
{

    private static readonly Range<ILocation> loc = new(new FakeLocation(), new FakeLocation());

    private static Identifier Ident(string name) => new(name, loc);

    #region Variable

    public static VariableDeclaration Var(string name) => Var(name, out _);

    public static VariableDeclaration Var(string name, out VariableDeclaration result) => result = new VariableDeclaration(
        Ident(name),
        Type: null,
        InitValue: null,
        IsConst: false,
        loc);

    public static VariableDeclaration Var(string name, int initValue) => Var(name, initValue, out _);

    public static VariableDeclaration Var(string name, int initValue, out VariableDeclaration result) =>
        result = new VariableDeclaration(
            Ident(name),
            Type: null,
            InitValue: Literal(initValue),
            IsConst: false,
            loc);

    public static VariableDeclaration Var(string name, bool initValue) => Var(name, initValue, out _);

    public static VariableDeclaration Var(string name, bool initValue, out VariableDeclaration result) =>
        result = new VariableDeclaration(
            Ident(name),
            Type: null,
            InitValue: Literal(initValue),
            IsConst: false,
            loc);

    public static VariableDeclaration Var<T>(string name, Expression initValue) where T : Type, new() => Var<T>(name, initValue, out _);

    public static VariableDeclaration Var<T>(string name, Expression initValue, out VariableDeclaration result)
        where T : Type, new() =>
        result = new VariableDeclaration(
            Ident(name),
            Type: new T(),
            InitValue: initValue,
            IsConst: false,
            loc);

    public static VariableDeclaration Var<T>(string name) where T : Type, new() => Var<T>(name, out _);

    public static VariableDeclaration Var<T>(string name, out VariableDeclaration result) where T : Type, new() =>
        result = new VariableDeclaration(
            Ident(name),
            Type: new T(),
            InitValue: null,
            IsConst: false,
            loc);

    public static VariableValue Value(string name) => Value(name, out _);

    public static VariableValue Value(string name, out VariableValue result) =>
        result = new VariableValue(Ident(name), loc);

    #endregion

    #region Function Call

    public static FuncCallBuilder Call(this string name) => new(Ident(name));

    public static FunctionCall Call(this string name, out FunctionCall result) => result = name.Call();

    public sealed class FuncCallBuilder
    {
        private readonly Identifier _identifier;
        private readonly List<Expression> _arguments = new();

        internal FuncCallBuilder(Identifier identifier) => _identifier = identifier;

        public FuncCallBuilder Argument(Expression arg)
        {
            _arguments.Add(arg);
            return this;
        }

        public static implicit operator FunctionCall(FuncCallBuilder builder) => new(
            builder._identifier,
            builder._arguments,
            loc);

        public FunctionCall Get(out FunctionCall result) => result = this;
    }

    #endregion

    #region Operators

    public static IntLiteralValue Literal(int value) => new(value, loc);

    private static BoolLiteralValue Literal(bool value) => new(value, loc);

    public static Infix Plus(this string name, int v) => Value(name).Plus(v);

    public static Infix Plus(this Expression e1, int v2) => e1.Plus(Literal(v2));

    private static Infix Plus(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.Plus, loc);

    public static Assignment Assign(this string name, int value) => name.Assign(value, out _);

    public static Assignment Assign(this string name, int value, out Assignment result) =>
        name.Assign(Literal(value), out result);

    public static Assignment Assign(this string name, Expression value) => name.Assign(value, out _);

    public static Assignment Assign(this string name, Expression value, out Assignment result) =>
        result = new Assignment(Ident(name), value, loc);

    #endregion

    #region Expressions

    public static Expression Program(params Expression[] lines) => lines.Join();

    private static ExpressionJoin Join(this Expression e1, Expression e2) => new(e1, e2, loc);

    private static Expression Join(this IEnumerable<Expression> expressions) =>
        expressions.Aggregate((sequence, expr) => sequence.Join(expr));

    public static CodeBlock Block(params Expression[] expressions) => new(expressions.Join(), loc);

    public static ReturnStatement Return(int value) => Return(Literal(value));

    public static ReturnStatement Return(Expression e) => new(e, loc);

    public static EmptyExpression Close => new(loc);

    #endregion

    #region Function Declaration

    public static FuncDeclarationBuilder Fun<ReturnType>(string name) where ReturnType : Type, new() =>
        new(Ident(name), new ReturnType());

    public sealed class FuncDeclarationBuilder
    {
        private readonly Identifier _identifier;
        private readonly Type _returnType;
        private readonly List<FunctionParameterDeclaration> _parameters = new();
        private CodeBlock? _body;

        internal FuncDeclarationBuilder(Identifier identifier, Type returnType) =>
            (_identifier, _returnType) = (identifier, returnType);

        public FuncDeclarationBuilder Parameter<ParamType>(string name) where ParamType : Type, new() =>
            Parameter<ParamType>(name, out _);

        public FuncDeclarationBuilder Parameter<ParamType>(string name, out FunctionParameterDeclaration result)
            where ParamType : Type, new()
        {
            _parameters.Add(result = new FunctionParameterDeclaration(Ident(name), new ParamType(), null, loc));
            return this;
        }

        public FuncDeclarationBuilder Body(params Expression[] expressions)
        {
            _body = new CodeBlock(expressions.Join(), loc);
            return this;
        }

        public static implicit operator FunctionDefinition(FuncDeclarationBuilder builder) => new(
            builder._identifier,
            builder._parameters,
            builder._returnType,
            builder._body ?? throw new InvalidOperationException(".Body() was not called yet"),
            loc);

        public FunctionDefinition Get(out FunctionDefinition result) => result = this;
    }

    #endregion
}
