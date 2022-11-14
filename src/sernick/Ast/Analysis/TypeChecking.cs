namespace sernick.Ast.Analysis;

using System.Data;
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

        protected override TypeInformation VisitAstNode(AstNode node, Dictionary<Expression, Type> partialExpressionTypes)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }

            return Unit.I;
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

        public override TypeInformation VisitAssignment(Assignment node, TypeInformation partialExpressiontypes)
        {
            
            var typeOfLeftSide = partialExpressiontypes[node.LeftSide];
            var typeOfRightSide = partialExpressiontypes[node.RightSide];
            if (typeOfLeftSide.ToString() != typeOfLeftSide.ToString())
            {
                this._diagnostics.Report(new TypeCheckingError(typeOfLeftSide, typeOfRightSide, node.LocationRange.Start));
            }

            // Regardless of the error, let's return a Unit type for assignment and get more type checking information
            var result = new TypeInformation(partialExpressiontypes);
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