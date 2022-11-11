namespace sernick.Ast.Analysis.NameResolution;

using Diagnostics;
using Errors;
using Nodes;
using Utility;

public sealed class Algorithm
{
    /// <summary>
    /// Maps uses of variables to the declarations
    /// of these variables
    /// </summary>
    public IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations
    {
        get;
        init;
    }

    /// <summary>
    /// Maps left-hand sides of assignments to variables
    /// to the declarations of these variables.
    /// NOTE: Since function parameters are non-assignable,
    /// these can only be variable declarations (`var x`, `const y`)
    /// </summary>
    public IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations
    {
        get;
        init;
    }

    /// <summary>
    /// Maps AST nodes for function calls
    /// to that function's declaration
    /// </summary>
    public IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations
    {
        get;
        init;
    }

    public Algorithm(AstNode ast, IDiagnostics diagnostics)
    {
        var visitor = new NameResolvingAstVisitor(diagnostics);
        var result = visitor.VisitAstTree(ast, new LocalVariablesManager(diagnostics));
        UsedVariableDeclarations = result.PartialAlgorithmResult.UsedVariableDeclarations;
        AssignedVariableDeclarations = result.PartialAlgorithmResult.AssignedVariableDeclarations;
        CalledFunctionDeclarations = result.PartialAlgorithmResult.CalledFunctionDeclarations;
    }

    
    private class NameResolvingAstVisitor : AstVisitor<VisitorResult, LocalVariablesManager>
    {
        private readonly IDiagnostics _diagnostics;
        public NameResolvingAstVisitor(IDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }
        protected override VisitorResult VisitAstNode(AstNode node, LocalVariablesManager variablesManager)
        {
            var partialResult = new PartialAlgorithmResult();
            foreach (var child in node.Children)
            {
                var childResult = child.Accept(this, variablesManager);
                partialResult = PartialAlgorithmResult.Join(partialResult, childResult.PartialAlgorithmResult);
                variablesManager = childResult.variablesManager;
            }

            return new VisitorResult(partialResult, variablesManager);
        }


        public override VisitorResult VisitVariableDeclaration(VariableDeclaration node, LocalVariablesManager variablesManager)
        {
            var visibleVariables = variablesManager.Add(node);
            return new VisitorResult(new PartialAlgorithmResult(), visibleVariables);
        }

        public override VisitorResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node,
            LocalVariablesManager variablesManager)
        {
            var visibleVariables = variablesManager.Add(node);
            return new VisitorResult(new PartialAlgorithmResult(), visibleVariables);
        }

        public override VisitorResult VisitFunctionDefinition(FunctionDefinition node, LocalVariablesManager variablesManager)
        {
            var visibleVariables = variablesManager.Add(node);
            var variablesInsideFunction = visibleVariables.NewScope();
            foreach (var parameter in node.Parameters)
            {
                variablesInsideFunction = parameter.Accept(this, variablesInsideFunction).variablesManager;
            }

            var visitorResult = node.Body.Accept(this, variablesInsideFunction);
            
            return new VisitorResult(visitorResult.PartialAlgorithmResult, visibleVariables);
        }

        public override VisitorResult VisitCodeBlock(CodeBlock node, LocalVariablesManager variablesManager)
        {
            var visitorResult = node.Inner.Accept(this, variablesManager.NewScope());
            return new VisitorResult(visitorResult.PartialAlgorithmResult, variablesManager);
        }


        public override VisitorResult VisitFunctionCall(FunctionCall node, LocalVariablesManager variablesManager)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.FunctionName, variablesManager);
            if (declaration == null)
            {
                return new VisitorResult(variablesManager);
            }
            return new VisitorResult(PartialAlgorithmResult.OfCalledFunction(node, (FunctionDefinition)declaration),
                variablesManager);
        }


        public override VisitorResult VisitAssignment(Assignment node, LocalVariablesManager variablesManager)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Left, variablesManager);
            if (declaration == null)
            {
                return new VisitorResult(variablesManager);
            }
            return new VisitorResult(PartialAlgorithmResult.OfAssignment(node, (VariableDeclaration)declaration),
                variablesManager);
        }

        public override VisitorResult VisitVariableValue(VariableValue node, LocalVariablesManager variablesManager)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Identifier, variablesManager);
            if (declaration == null)
            {
                return new VisitorResult(variablesManager);
            }
            return new VisitorResult(PartialAlgorithmResult.OfUsedVariable(node, declaration),
                variablesManager);
        }

        private Declaration? GetDeclarationAndReportIfMissing(Identifier identifier, LocalVariablesManager variablesManager)
        {
            var name = identifier.Name;
            if (variablesManager.Variables.ContainsKey(name))
            {
                return variablesManager.Variables[name];
            }
            else
            {
                _diagnostics.Report(new UndeclaredIdentifierError(identifier));
                return null;
            }
        }
    }
}
