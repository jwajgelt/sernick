namespace sernickTest.Ast.Helpers;

using System.Drawing;
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

    public static Identifier Ident(string name) => Ident(name, out _);
    public static Identifier Ident(string name, out Identifier result) => result = new(name, loc);
    

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

    public static VariableDeclaration Var(string name, Type type) => Var(name, type, out _);

    public static VariableDeclaration Var(string name, Type type, out VariableDeclaration result) =>
        result = new VariableDeclaration(
            Ident(name),
            Type: type,
            InitValue: null,
            IsConst: false,
            loc);


    public static VariableDeclaration Var<T>(string name, bool initValue) where T : Type, new() => Var<T>(name, initValue, out _);

    public static VariableDeclaration Var<T>(string name, bool initValue, out VariableDeclaration result) where T : Type, new() =>
        Var<T>(name, Literal(initValue), out result);

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

    public static BoolLiteralValue Literal(bool value) => new(value, loc);

    public static Infix Plus(this string name, int v) => Value(name).Plus(v);

    public static Infix Plus(this string name1, string name2) => Value(name1).Plus(Value(name2));

    public static Infix Plus(this Expression e1, string name2) => e1.Plus(Value(name2));

    public static Infix Plus(this Expression e1, int v2) => e1.Plus(Literal(v2));

    public static Infix Plus(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.Plus, loc);

    public static Infix Minus(this string name, int v) => Value(name).Minus(v);

    public static Infix Minus(this Expression e1, int v2) => e1.Minus(Literal(v2));

    public static Infix Minus(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.Minus, loc);

    public static Infix Eq(this string name, int v) => name.Eq(Literal(v));

    public static Infix Eq(this string name1, string name2) => Value(name1).Eq(Value(name2));

    public static Infix Eq(this string name, Expression e2) => Value(name).Eq(e2);

    public static Infix Eq(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.Equals, loc);

    public static Infix Leq(this string name, int v) => Value(name).Leq(Literal(v));

    public static Infix Leq(this string name1, string name2) => Value(name1).Leq(Value(name2));

    public static Infix Leq(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.LessOrEquals, loc);

    public static Infix ScOr(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.ScOr, loc);

    public static Infix ScAnd(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.ScAnd, loc);

    public static Assignment Assign(this string name, int value) => name.Assign(value, out _);

    public static Assignment Assign(this string name, int value, out Assignment result) =>
        name.Assign(Literal(value), out result);

    public static Assignment Assign(this string name, Expression value) => name.Assign(value, out _);

    public static Assignment Assign(this string name, Expression value, out Assignment result) =>
        result = new Assignment(Value(name), value, loc);

    public static Assignment Assign(this Expression left, Expression value) => Assign(left, value, out _);

    public static Assignment Assign(this Expression left, Expression value, out Assignment result) =>
        result = new Assignment(left, value, loc);

    #endregion

    #region Expressions

    public static FunctionDefinition Program(params Expression[] lines) => new(
        Name: Ident(""),
        Parameters: Array.Empty<FunctionParameterDeclaration>(),
        ReturnType: new UnitType(),
        Body: Block(lines),
        LocationRange: loc);

    private static ExpressionJoin Join(this Expression e1, Expression e2) => new(e1, e2, loc);

    private static Expression Join(this IEnumerable<Expression> expressions) =>
        expressions.Aggregate((sequence, expr) => sequence.Join(expr));

    public static CodeBlock Block(params Expression[] expressions) => new(expressions.Join(), loc);

    public static Expression Group(params Expression[] expressions) => expressions.Join();

    public static ReturnStatement Return(int value) => Return(Literal(value));

    public static ReturnStatement Return(bool value) => Return(Literal(value));

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

        public FuncDeclarationBuilder Parameter(string name, bool defaultValue) =>
            Parameter<BoolType>(name, Literal(defaultValue));

        public FuncDeclarationBuilder Parameter(string name, int defaultValue) =>
            Parameter<IntType>(name, Literal(defaultValue));

        public FuncDeclarationBuilder Parameter<ParamType>(string name, LiteralValue defaultValue) where ParamType : Type, new() =>
            Parameter<ParamType>(name, defaultValue, out _);

        public FuncDeclarationBuilder Parameter<ParamType>(string name, LiteralValue defaultValue, out FunctionParameterDeclaration result)
            where ParamType : Type, new()
        {
            _parameters.Add(result = new FunctionParameterDeclaration(Ident(name), new ParamType(), defaultValue, loc));
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

    #region Control Flow

    public static IfStatementBuilder If(Expression condition) => new(condition);

    public static IfStatementBuilder If(bool value) => new(Literal(value));

    public sealed class IfStatementBuilder
    {
        private readonly Expression _condition;
        private CodeBlock? _ifBlock;
        private CodeBlock? _elseBlock;

        internal IfStatementBuilder(Expression condition) => _condition = condition;

        public IfStatementBuilder Then(params Expression[] expressions)
        {
            _ifBlock = new CodeBlock(expressions.Join(), loc);
            return this;
        }

        public IfStatementBuilder Else(params Expression[] expressions)
        {
            _elseBlock = new CodeBlock(expressions.Join(), loc);
            return this;
        }

        public static implicit operator IfStatement(IfStatementBuilder builder) => new(
            builder._condition,
            builder._ifBlock ?? throw new InvalidOperationException(".Then() was not called yet"),
            builder._elseBlock,
            loc
        );

        public IfStatement Get(out IfStatement result) => result = this;
    }

    public static LoopStatement Loop(params Expression[] expressions) => new(Block(expressions), loc);

    public static BreakStatement Break => new(loc);

    #endregion

    #region Loop
    public static LoopStatement Loop(CodeBlock codeBlock) => new LoopStatement(codeBlock, loc);
    #endregion

    #region Pointers
    public static PointerType Pointer(Identifier type) => new PointerType(new StructType(type));
    public static FunctionCall Alloc(Expression arg) => Call("new").Argument(arg);
    public static PointerDereference Deref(Expression pointer) => new PointerDereference(pointer, loc);
    public static NullPointerLiteralValue Null => new(loc);
    #endregion

    #region Struct
    public static StructDeclarationBuilder Struct(string name) => new(Ident(name));
    public sealed class StructDeclarationBuilder
    {
        private readonly Identifier _identifier;
        private readonly List<FieldDeclaration> _fields = new();

        internal StructDeclarationBuilder(Identifier identifier) => _identifier = identifier;

        public StructDeclarationBuilder Field(FieldDeclaration field)
        {
            _fields.Add(field);
            return this;
        }

        public StructDeclarationBuilder Field(string name, Type type)
        {
            _fields.Add(new FieldDeclaration(Ident(name), type, loc));
            return this;
        }

        public static implicit operator StructDeclaration(StructDeclarationBuilder builder) => new(
            builder._identifier,
            builder._fields,
            loc
        );

        public StructDeclaration Get(out StructDeclaration result) => result = this;
    }

    public static StructValueBuilder StructValue(string name) => new(Ident(name));
    public sealed class StructValueBuilder
    {
        private readonly Identifier _identifier;
        private readonly List<StructFieldInitializer> _fields = new();

        internal StructValueBuilder(Identifier identifier) => _identifier = identifier;

        public StructValueBuilder Field(StructFieldInitializer field)
        {
            _fields.Add(field);
            return this;
        }

        public StructValueBuilder Field(string name, Expression value)
        {
            _fields.Add(new StructFieldInitializer(Ident(name), value, loc));
            return this;
        }

        public static implicit operator StructValue(StructValueBuilder builder) => new(
            builder._identifier,
            builder._fields,
            loc
        );

        public StructValue Get(out StructValue result) => result = this;
    }

    public static StructFieldAccess Field(this Expression left, string name) => new StructFieldAccess(left, Ident(name), loc);
    #endregion
}
