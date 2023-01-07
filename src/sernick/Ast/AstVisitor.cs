namespace sernick.Ast;

using Nodes;

/// <summary>
/// A base class for AST tree visitors. A typical implementation would override only the Visit() methods it is interested in.
/// It is implementor's responsibility to visit children (or siblings) recursively: e.g.
/// <code>
/// foreach (var child in node.Children) {
///     child.Accept(this, param);
/// }
/// </code>
/// </summary>
public abstract class AstVisitor<TResult, TParam>
{
    public TResult VisitAstTree(AstNode node, TParam param) => node.Accept(this, param);

    protected abstract TResult VisitAstNode(AstNode node, TParam param);

    protected virtual TResult VisitExpression(Expression node, TParam param) => VisitAstNode(node, param);
    protected virtual TResult VisitDeclaration(Declaration node, TParam param) => VisitExpression(node, param);
    protected virtual TResult VisitFlowControlStatement(FlowControlStatement node, TParam param) => VisitExpression(node, param);
    protected virtual TResult VisitSimpleValue(SimpleValue node, TParam param) => VisitExpression(node, param);
    protected virtual TResult VisitLiteralValue(LiteralValue node, TParam param) => VisitSimpleValue(node, param);

    public virtual TResult VisitIdentifier(Identifier node, TParam param) => VisitAstNode(node, param);

    public virtual TResult VisitVariableDeclaration(VariableDeclaration node, TParam param) => VisitDeclaration(node, param);
    public virtual TResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, TParam param) => VisitDeclaration(node, param);
    public virtual TResult VisitFunctionDefinition(FunctionDefinition node, TParam param) => VisitDeclaration(node, param);

    public virtual TResult VisitCodeBlock(CodeBlock node, TParam param) => VisitExpression(node, param);
    public virtual TResult VisitExpressionJoin(ExpressionJoin node, TParam param) => VisitExpression(node, param);
    public virtual TResult VisitFunctionCall(FunctionCall node, TParam param) => VisitExpression(node, param);

    public virtual TResult VisitContinueStatement(ContinueStatement node, TParam param) => VisitFlowControlStatement(node, param);
    public virtual TResult VisitReturnStatement(ReturnStatement node, TParam param) => VisitFlowControlStatement(node, param);
    public virtual TResult VisitBreakStatement(BreakStatement node, TParam param) => VisitFlowControlStatement(node, param);
    public virtual TResult VisitIfStatement(IfStatement node, TParam param) => VisitFlowControlStatement(node, param);
    public virtual TResult VisitLoopStatement(LoopStatement node, TParam param) => VisitFlowControlStatement(node, param);

    public virtual TResult VisitInfix(Infix node, TParam param) => VisitExpression(node, param);
    public virtual TResult VisitAssignment(Assignment node, TParam param) => VisitExpression(node, param);

    public virtual TResult VisitPointerDereference(PointerDereference node, TParam param) =>
        VisitExpression(node, param);

    public virtual TResult VisitVariableValue(VariableValue node, TParam param) => VisitSimpleValue(node, param);
    public virtual TResult VisitBoolLiteralValue(BoolLiteralValue node, TParam param) => VisitLiteralValue(node, param);
    public virtual TResult VisitIntLiteralValue(IntLiteralValue node, TParam param) => VisitLiteralValue(node, param);
    public virtual TResult VisitNullPointerLiteralValue(NullPointerLiteralValue node, TParam param) =>
        VisitLiteralValue(node, param);
    public virtual TResult VisitEmptyExpression(EmptyExpression node, TParam param) => VisitExpression(node, param);

    public virtual TResult VisitStructDeclaration(StructDeclaration node, TParam param) => VisitDeclaration(node, param);
    public virtual TResult VisitFieldDeclaration(FieldDeclaration node, TParam param) => VisitDeclaration(node, param);
    public virtual TResult VisitStructValue(StructValue node, TParam param) => VisitSimpleValue(node, param);
    public virtual TResult VisitStructFieldInitializer(StructFieldInitializer node, TParam param) => VisitAstNode(node, param);
    public virtual TResult VisitStructFieldAccess(StructFieldAccess node, TParam param) => VisitSimpleValue(node, param);
}
