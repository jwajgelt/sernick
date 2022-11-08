namespace sernick.Ast;

using Nodes;

public abstract class AstVisitor<TResult, TParam>
{
    protected abstract TResult VisitAstNode(AstNode node, TParam param);

    private TResult VisitExpression(Expression node, TParam param) => VisitAstNode(node, param);
    private TResult VisitDeclaration(Declaration node, TParam param) => VisitExpression(node, param);
    private TResult VisitFlowControlStatement(FlowControlStatement node, TParam param) => VisitExpression(node, param);
    private TResult VisitSimpleValue(SimpleValue node, TParam param) => VisitExpression(node, param);
    private TResult VisitLiteralValue(LiteralValue node, TParam param) => VisitSimpleValue(node, param);

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

    public virtual TResult VisitVariableValue(VariableValue node, TParam param) => VisitSimpleValue(node, param);
    public virtual TResult VisitBoolLiteralValue(BoolLiteralValue node, TParam param) => VisitLiteralValue(node, param);
    public virtual TResult VisitIntLiteralValue(IntLiteralValue node, TParam param) => VisitLiteralValue(node, param);
}
