namespace sernick.Ast.Analysis;

using System.Data;
using System.Xml.Linq;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;

using TypeInformation = Dictionary<Ast.Nodes.AstNode, Type>;

public sealed class TypeChecking
{
    /// <summary>
    /// Maps expressions to their types
    /// </summary>
    public IReadOnlyDictionary<Expression, Type> ExpressionTypes
    {
        get;
        init;
    }

    public TypeChecking(AstNode ast, NameResolutionResult nameResolution, Diagnostics diagnostics)
    {
        // TODO: implement a TypeCheckingASTVisitor, walk it over the AST and initialize the property with the result
        var visitor = new TypeCheckingAstVisitor(nameResolution, diagnostics);
        var newExpressionTypes = new Dictionary<Expression, Type>();
    }

    // TODO: use correct param and result types
    private class TypeCheckingAstVisitor : AstVisitor<TypeInformation, Dictionary<AstNode, Type>>
    {
        /// <summary>
        /// Invariant: when visiting AstNode X, types for all children of X are known
        /// and stored in partialExpressionTypes dictionary
        /// </summary>
        private IReadOnlyDictionary<AstNode, Type> partialExpressionTypes;
        private readonly NameResolutionResult nameResolution;
        private Diagnostics _diagnostics;

        public TypeCheckingAstVisitor(NameResolutionResult nameResolution, Diagnostics diagnostics)
        {
            this.nameResolution = nameResolution;
            this.partialExpressionTypes = new TypeInformation();
            this._diagnostics = diagnostics;
        }

        public override TypeInformation VisitCodeBlock(CodeBlock node, TypeInformation param)
        {
            // simply return what expression inside returns?
            var result = new TypeInformation(param);
            result.Add(node, param[node.Inner]);
            return result;

        }

        public override TypeInformation VisitExpressionJoin(ExpressionJoin node, TypeInformation param)
        {
            var result = new TypeInformation(param);
            result.Add(node, param[node.Second]); // just return the last expressions' type
            return result;
        }

        public override TypeInformation VisitFunctionCall(FunctionCall functionCallNode, TypeInformation param)
        {
            var functionDeclarationNode = nameResolution.CalledFunctionDeclarations[functionCallNode];
            var declaredReturnType = functionDeclarationNode.ReturnType;

            var inferredReturnType = new UnitType(); // TODO

            if(inferredReturnType.ToString() != declaredReturnType.ToString())
            {
                this._diagnostics.Report(new TypeCheckingError(declaredReturnType, inferredReturnType, functionCallNode.LocationRange.Start));
                return param; // not sure here
            }

            var result = new TypeInformation(param);
            result.Add(functionCallNode, new UnitType()); // function call returns a unit, not a function return type
            return result;

        }

        public override TypeInformation VisitContinueStatement(ContinueStatement node, TypeInformation param)
        {
            var result = new TypeInformation(param);
            result.Add(node, new UnitType());
            return result;
        }

        public override TypeInformation VisitReturnStatement(ReturnStatement node, TypeInformation param)
        {
            var result = new TypeInformation(param);
            // Return Value is in a subtree, so its type should be already calculated by now
            result.Add(node, param[node.ReturnValue]);
            return result;
        }

        public override TypeInformation VisitBreakStatement(BreakStatement node, TypeInformation partialExpressionTypes)
        {
            var result = new TypeInformation(partialExpressionTypes);
            result.Add(node, new UnitType());
            return result;
        }


        public override TypeInformation VisitIfStatement(IfStatement node, TypeInformation param)
        {
            // TODO
            return null;
        }

        public override TypeInformation VisitLoopStatement(LoopStatement node, TypeInformation partialExpressionTypes)
        {
            // TODO what if Loop contains a "return 12;"?
            var result = new TypeInformation(partialExpressionTypes);
            result.Add(node, new UnitType());
            return result;
        }


        public override TypeInformation VisitInfix(Infix node, TypeInformation partialTypeInformation)
        {
            var typeOfLeftOperand = partialTypeInformation[node.LeftSide];
            var typeOfRightOperand = partialTypeInformation[node.RightSide];

            if(typeOfLeftOperand.ToString() != typeOfRightOperand.ToString())
            {
                // TODO we probably should create a separate error class for operator type mismatch
                this._diagnostics.Report(new TypeCheckingError(typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));

                // TODO does it make sense to return anything here? maybe a Unit type? But it could propagate the error up the tree 
                var result = new TypeInformation(partialTypeInformation);
                result.Add(node, new UnitType());
                return result;
            }
            else
            {
                var commonType = typeOfLeftOperand;
                var result = new TypeInformation(partialTypeInformation);
                result.Add(node, commonType);
                return result;
            }
        }

        public override TypeInformation VisitAssignment(Assignment node, TypeInformation partialExpressionTypes)
        {
            
            var typeOfLeftSide = partialExpressionTypes[node.LeftSide];
            var typeOfRightSide = partialExpressionTypes[node.RightSide];
            if (typeOfLeftSide.ToString() != typeOfLeftSide.ToString())
            {
                this._diagnostics.Report(new TypeCheckingError(typeOfLeftSide, typeOfRightSide, node.LocationRange.Start));
            }

            // Regardless of the error, let's return a Unit type for assignment and get more type checking information
            var result = new TypeInformation(partialExpressionTypes);
            result.Add(node, new UnitType());
            return result;
        }

        public override TypeInformation VisitVariableValue(VariableValue node, TypeInformation partialExpressiontypes)
        {
            var result = new TypeInformation(partialExpressiontypes);

            var variableDeclarationNode = this.nameResolution.UsedVariableDeclarations[node];
            var variableHasExplicitType = variableDeclarationNode.Type != null;

            // explicit type near variable declaration
            if(variableDeclarationNode.Type != null)
            {
                result.Add(node, variableDeclarationNode.Type);
                return result;
            }

            // if there is no type, variable must have been immediately initialized (language-wide decision)
            // but we should check it for not being equal null just so C# compiler is satisfied
            if (variableDeclarationNode.InitValue != null)
            {
                var typeInformation2 = VisitExpression(variableDeclarationNode.InitValue, partialExpressiontypes);
                var typeOfExpression = typeInformation2[variableDeclarationNode.InitValue];

                result.Add(node, typeOfExpression);
                return result;
            }

            // this "return" should never be reached
            return result;
        }


        public override TypeInformation VisitBoolLiteralValue(BoolLiteralValue node, TypeInformation partialExpressiontypes)
        {
            var result = new TypeInformation(partialExpressiontypes);
            result.Add(node, new BoolType());
            return result;
        }

        public override TypeInformation VisitIntLiteralValue(IntLiteralValue node, TypeInformation partialExpressiontypes)
        {
            var result = new TypeInformation(partialExpressiontypes);
            result.Add(node, new IntType());
            return result;
        }

    }
}