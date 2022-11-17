namespace sernickTest.Ast.Helpers;

using sernick.Ast;
using sernick.Ast.Nodes;
using sernick.Input;
using sernick.Utility;
using Tokenizer.Lexer.Helpers;

public static class AstNodesExtensions {
    private static readonly Range<ILocation> loc = new(new FakeInput.Location(0), new FakeInput.Location(0));

    public static Identifier Ident(string name) => new(name, loc);

    public static VariableDeclaration Var(string name) => Var(name, out _);

    public static VariableDeclaration Var(string name, out VariableDeclaration result) => result = new(
        Ident(name),
        Type: null,
        InitValue: null,
        IsConst: false,
        loc);

    public static VariableValue Value(string name) => Value(name, out _);

    public static VariableValue Value(string name, out VariableValue result) => result = new(Ident(name), loc);

    public static FunctionCall Call(this string name) => name.Call(out _);

    public static FunctionCall Call(this string name, out FunctionCall result) =>
        result = new(Ident(name), Array.Empty<Expression>(), loc);

    public static Infix Plus(this Expression e1, Expression e2) => new(e1, e2, Infix.Op.Plus, loc);

    public static IntLiteralValue Literal(int value) => new(value, loc);

    public static Infix Plus(this Expression e1, int v2) => new(e1, Literal(v2), Infix.Op.Plus, loc);

    public static Infix Plus(this string name, int v) => Value(name).Plus(v);

    public static Assignment Assign(this Identifier identifier, int value) => identifier.Assign(value, out _);

    public static Assignment Assign(this Identifier identifier, int value, out Assignment result) =>
        result = new(identifier, Literal(value), loc);

    public static Assignment Assign(this string name, int value) =>
        name.Assign(value, out _);

    public static Assignment Assign(this string name, int value, out Assignment result) =>
        Ident(name).Assign(value, out result);

    public static ExpressionJoin Join(this Expression e1, Expression e2) => new(e1, e2, loc);

    public static CodeBlock Block(params Expression[] exprs) => new(exprs.Join(), loc);

    public static ReturnStatement Return(int value) => Return(Literal(value));

    public static ReturnStatement Return(Expression e) => new(e, loc);

    public static FuncDeclarationBuilder Fun<ReturnType>(string name) where ReturnType : Type, new() =>
        new(Ident(name), new ReturnType());

    public sealed class FuncDeclarationBuilder {
        private readonly Identifier _identifier;
        private readonly Type _returnType;
        private readonly List<FunctionParameterDeclaration> _parameters = new();
        private CodeBlock? _body = null;

        internal FuncDeclarationBuilder(Identifier identifier, Type returnType) {
            _identifier = identifier;
            _returnType = returnType;
        }

        public FuncDeclarationBuilder Parameter<ParamType>(string name)
            where ParamType : Type, new() => Parameter<ParamType>(name, out _);

        public FuncDeclarationBuilder Parameter<ParamType>(string name, out FunctionParameterDeclaration result)
            where ParamType : Type, new()
        {
            _parameters.Add(result = new FunctionParameterDeclaration(Ident(name), new ParamType(), null, loc));
            return this;
        }

        public FuncDeclarationBuilder Body(params Expression[] exprs) {
            _body = new CodeBlock(exprs.Join(), loc);
            return this;
        }

        public FunctionDefinition Get(out FunctionDefinition result) => result = Get();

        public FunctionDefinition Get() => new(
            _identifier,
            _parameters,
            _returnType,
            _body ?? throw new InvalidOperationException(".Body() was not called yet"),
            loc);
    }

    public static Expression Close => new EmptyExpression(loc);

    private static Expression Join(this IEnumerable<Expression> expressions) {
        return expressions.Aggregate((seq, expr) => seq.Join(expr));
    }

    public static Expression Program(params Expression[] lines) => lines.Join();
}
