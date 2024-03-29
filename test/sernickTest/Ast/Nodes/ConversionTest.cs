namespace sernickTest.Ast.Nodes;

using Parser.Helpers;
using sernick.Ast;
using sernick.Ast.Nodes;
using sernick.Grammar.Lexicon;
using sernick.Grammar.Syntax;

public class ConversionTest
{

    private static FakeParseTreeNode Node(NonTerminalSymbol nonTerminal, params IFakeParseTree[] children)
        => new(Symbol.Of(nonTerminal), children);

    private static FakeParseTreeLeaf Leaf(LexicalGrammarCategory category, string text)
        => new(Symbol.Of(category, text));

    [Fact]
    public void Program_is_Converted_to_FunctionDefinition()
    {
        var parseTree = Node(NonTerminalSymbol.Program,
            Node(NonTerminalSymbol.ExpressionSeq, Array.Empty<IFakeParseTree>())
        ).Convert();

        var ast = AstNode.From(parseTree);
        Assert.True(ast is FunctionDefinition
        {
            Name.Name: "",
            ReturnType: UnitType,
            Body.Inner: EmptyExpression
        });
        var parameters = ((FunctionDefinition)ast).Parameters;
        Assert.Empty(parameters);
    }

    [Fact]
    public void EmptyExpression_Conversion()
    {
        var parseTree = Node(NonTerminalSymbol.ExpressionSeq, Array.Empty<IFakeParseTree>()).Convert();
        Assert.True(AstNode.From(parseTree) is EmptyExpression);
    }

    [Fact]
    public void BreakKeyword_Conversion()
    {
        var input = Leaf(LexicalGrammarCategory.Keywords, "break").Convert();
        Assert.True(AstNode.From(input) is BreakStatement);
    }

    [Fact]
    public void ContinueKeyword_Conversion()
    {
        var input = Leaf(LexicalGrammarCategory.Keywords, "continue").Convert();
        Assert.True(AstNode.From(input) is ContinueStatement);
    }

    [Fact]
    public void ReturnExpression_Conversion()
    {
        var input = Node(NonTerminalSymbol.ReturnExpression,
            Leaf(LexicalGrammarCategory.Keywords, "return")).Convert();
        Assert.True(AstNode.From(input) is ReturnStatement { ReturnValue: null });
    }

    [Fact]
    public void Bracketed_OpenedExpression_Conversion()
    {
        // { x }
        var input = Node(NonTerminalSymbol.CodeBlock,
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
            Node(NonTerminalSymbol.ExpressionSeq,
                Node(NonTerminalSymbol.OpenExpression,
                    Node(NonTerminalSymbol.LogicalOperand,
                        Node(NonTerminalSymbol.ComparisonOperand,
                            Node(NonTerminalSymbol.ArithmeticOperand,
                                Node(NonTerminalSymbol.SimpleExpression,
                                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")
                                    )))))),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")
            ).Convert();

        Assert.True(AstNode.From(input) is CodeBlock { Inner: VariableValue { Identifier.Name: "x" } });
    }

    [Fact]
    public void Bracketed_ClosedExpression_Conversion()
    {
        // { x; }
        var input = Node(NonTerminalSymbol.CodeBlock,
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
            Node(NonTerminalSymbol.ExpressionSeq,
                Node(NonTerminalSymbol.OpenExpression,
                    Node(NonTerminalSymbol.LogicalOperand,
                        Node(NonTerminalSymbol.ComparisonOperand,
                            Node(NonTerminalSymbol.ArithmeticOperand,
                                Node(NonTerminalSymbol.SimpleExpression,
                                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")
                                ))))),
                Leaf(LexicalGrammarCategory.Semicolon, ";")
                ),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")
        ).Convert();

        // Converter should add Unit type after "x" so that the CodeBlock has the right type
        Assert.True(AstNode.From(input) is CodeBlock
        {
            Inner: ExpressionJoin
            {
                First: VariableValue { Identifier.Name: "x" },
                Second: EmptyExpression
            }
        });
    }

    [Fact]
    public void Bracketed_BlockExpression_Conversion()
    {
        // { if (x) {} else {} }
        var input = Node(NonTerminalSymbol.CodeBlock,
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
            Node(NonTerminalSymbol.ExpressionSeq,
                Node(NonTerminalSymbol.IfExpression,
                    Leaf(LexicalGrammarCategory.Keywords, "if"),
                    Node(NonTerminalSymbol.IfCondition,
                        Leaf(LexicalGrammarCategory.BracesAndParentheses, "("),
                        Node(NonTerminalSymbol.OpenExpression,
                            Node(NonTerminalSymbol.LogicalOperand,
                                Node(NonTerminalSymbol.ComparisonOperand,
                                    Node(NonTerminalSymbol.ArithmeticOperand,
                                        Node(NonTerminalSymbol.SimpleExpression,
                                            Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")))))),
                        Leaf(LexicalGrammarCategory.BracesAndParentheses, ")")),
                    Node(NonTerminalSymbol.CodeBlock,
                        Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
                        Node(NonTerminalSymbol.ExpressionSeq),
                        Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")),
                    Leaf(LexicalGrammarCategory.Keywords, "else"),
                    Node(NonTerminalSymbol.CodeBlock,
                        Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
                        Node(NonTerminalSymbol.ExpressionSeq),
                        Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")))),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")
        ).Convert();

        // Converter should not add Unit expression after if-expr
        Assert.True(AstNode.From(input) is CodeBlock
        {
            Inner: IfStatement
            {
                Condition: VariableValue { Identifier.Name: "x" },
                IfBlock.Inner: EmptyExpression,
                ElseBlock.Inner: EmptyExpression
            }
        });
    }

    [Fact]
    public void ArithmeticExpression_Conversion()
    {
        // 1 + 2
        var input = Node(NonTerminalSymbol.ComparisonOperand,
            Node(NonTerminalSymbol.ArithmeticOperand,
                Node(NonTerminalSymbol.SimpleExpression,
                    Node(NonTerminalSymbol.LiteralValue,
                        Leaf(LexicalGrammarCategory.Literals, "1")))),
            Node(NonTerminalSymbol.ArithmeticOperator,
                Leaf(LexicalGrammarCategory.Operators, "+")),
            Node(NonTerminalSymbol.ArithmeticOperand,
                Node(NonTerminalSymbol.SimpleExpression,
                    Node(NonTerminalSymbol.LiteralValue,
                        Leaf(LexicalGrammarCategory.Literals, "2"))))
        ).Convert();

        Assert.True(AstNode.From(input) is Infix
        {
            Left: IntLiteralValue { Value: 1 },
            Right: IntLiteralValue { Value: 2 },
            Operator: Infix.Op.Plus
        });
    }

    [Fact]
    public void FunctionDeclaration_Conversion()
    {
        // fun foo(x: Int = 1) { x }
        var input = Node(NonTerminalSymbol.FunctionDeclaration,
            Leaf(LexicalGrammarCategory.Keywords, "fun"),
            Leaf(LexicalGrammarCategory.VariableIdentifiers, "foo"),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "("),
            Node(NonTerminalSymbol.FunctionParameters,
                Node(NonTerminalSymbol.FunctionParameterWithDefaultValue,
                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "x"),
                    Node(NonTerminalSymbol.TypeSpecification,
                        Leaf(LexicalGrammarCategory.Colon, ":"),
                        Leaf(LexicalGrammarCategory.TypeIdentifiers, "Int")
                    ),
                    Leaf(LexicalGrammarCategory.Operators, "="),
                    Node(NonTerminalSymbol.LiteralValue,
                        Leaf(LexicalGrammarCategory.Literals, "1")
                    )
                )
            ),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, ")"),
            Node(NonTerminalSymbol.CodeBlock,
                Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
                Node(NonTerminalSymbol.ExpressionSeq,
                    Node(NonTerminalSymbol.SimpleExpression,
                        Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")
                    )
                ),
                Leaf(LexicalGrammarCategory.BracesAndParentheses, "}"))
        ).Convert();
        var astNode = AstNode.From(input);

        Assert.True(astNode is FunctionDefinition
        {
            Name.Name: "foo",
            ReturnType: UnitType,
            Body.Inner: VariableValue { Identifier.Name: "x" }
        });

        var funAstNode = (FunctionDefinition)astNode;
        Assert.Single(funAstNode.Parameters);
        Assert.True(funAstNode.Parameters.First() is
        {
            Name.Name: "x",
            Type: IntType,
            DefaultValue: IntLiteralValue { Value: 1 }
        });
    }

    [Fact]
    public void StructDeclaration_Conversion()
    {
        // struct Struct { a: Int, b: Bool }
        var input = Node(NonTerminalSymbol.StructDeclaration,
            Leaf(LexicalGrammarCategory.Keywords, "struct"),
            Leaf(LexicalGrammarCategory.TypeIdentifiers, "Struct"),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
            Node(NonTerminalSymbol.StructDeclarationFields,
                Node(NonTerminalSymbol.StructFieldDeclaration,
                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "a"),
                    Node(NonTerminalSymbol.TypeSpecification,
                        Leaf(LexicalGrammarCategory.Colon, ":"),
                        Leaf(LexicalGrammarCategory.TypeIdentifiers, "Int")
                    )),
                Leaf(LexicalGrammarCategory.Comma, ","),
                Node(NonTerminalSymbol.StructFieldDeclaration,
                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "b"),
                    Node(NonTerminalSymbol.TypeSpecification,
                        Leaf(LexicalGrammarCategory.Colon, ":"),
                        Leaf(LexicalGrammarCategory.TypeIdentifiers, "Bool")
                    ))
            ),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")
        ).Convert();
        var astNode = AstNode.From(input);

        Assert.True(astNode is StructDeclaration
        {
            Name.Name: "Struct",
            Fields.Count: 2
        });

        var fields = ((StructDeclaration)astNode).Fields.ToList();
        Assert.True(fields[0] is
        {
            Name.Name: "a",
            Type: IntType
        });
        Assert.True(fields[1] is
        {
            Name.Name: "b",
            Type: BoolType
        });
    }

    [Fact]
    public void StructValue_Conversion()
    {
        // Struct { a: 0, b: false }
        var input = Node(NonTerminalSymbol.StructValue,
            Leaf(LexicalGrammarCategory.TypeIdentifiers, "Struct"),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
            Node(NonTerminalSymbol.StructValueFields,
                Node(NonTerminalSymbol.StructFieldInitializer,
                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "a"),
                    Leaf(LexicalGrammarCategory.Colon, ":"),
                    Node(NonTerminalSymbol.OpenExpression,
                        Node(NonTerminalSymbol.LogicalOperand,
                            Node(NonTerminalSymbol.ComparisonOperand,
                                Node(NonTerminalSymbol.ArithmeticOperand,
                                    Node(NonTerminalSymbol.SimpleExpression,
                                        Node(NonTerminalSymbol.LiteralValue,
                                            Leaf(LexicalGrammarCategory.Literals, "0")))))))),
                Leaf(LexicalGrammarCategory.Comma, ","),
                Node(NonTerminalSymbol.StructFieldInitializer,
                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "b"),
                    Leaf(LexicalGrammarCategory.Colon, ":"),
                    Node(NonTerminalSymbol.OpenExpression,
                        Node(NonTerminalSymbol.LogicalOperand,
                            Node(NonTerminalSymbol.ComparisonOperand,
                                Node(NonTerminalSymbol.ArithmeticOperand,
                                    Node(NonTerminalSymbol.SimpleExpression,
                                        Node(NonTerminalSymbol.LiteralValue,
                                            Leaf(LexicalGrammarCategory.Literals, "false"))))))))),
            Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")
        ).Convert();
        var astNode = AstNode.From(input);

        Assert.True(astNode is StructValue
        {
            StructName.Name: "Struct",
            Fields.Count: 2
        });

        var fields = ((StructValue)astNode).Fields.ToList();
        Assert.True(fields[0] is
        {
            FieldName.Name: "a",
            Value: IntLiteralValue { Value: 0 }
        });
        Assert.True(fields[1] is
        {
            FieldName.Name: "b",
            Value: BoolLiteralValue { Value: false }
        });
    }

    [Fact]
    public void PointerType_Conversion()
    {
        // var x: **Struct;
        var input = Node(NonTerminalSymbol.VariableDeclaration,
            Node(NonTerminalSymbol.Modifier,
                Leaf(LexicalGrammarCategory.Keywords, "var")),
            Leaf(LexicalGrammarCategory.VariableIdentifiers, "x"),
            Node(NonTerminalSymbol.TypeSpecification,
                Leaf(LexicalGrammarCategory.Semicolon, ":"),
                Node(NonTerminalSymbol.Type,
                    Leaf(LexicalGrammarCategory.Operators, "*"),
                    Node(NonTerminalSymbol.Type,
                        Leaf(LexicalGrammarCategory.Operators, "*"),
                        Node(NonTerminalSymbol.Type,
                            Leaf(LexicalGrammarCategory.TypeIdentifiers, "Struct")))
                ))
        ).Convert();

        Assert.True(AstNode.From(input) is VariableDeclaration
        {
            Name.Name: "x",
            Type: PointerType
            {
                Type: PointerType
                {
                    Type: StructType
                    {
                        Struct.Name: "Struct"
                    }
                }
            }
        });
    }

    [Fact]
    public void PointerDereference_Conversion()
    {
        // *(**y + 2)
        var input = Node(NonTerminalSymbol.OpenExpression,
            Node(NonTerminalSymbol.LogicalOperand,
                Node(NonTerminalSymbol.ComparisonOperand,
                    Node(NonTerminalSymbol.ArithmeticOperand,
                        Leaf(LexicalGrammarCategory.Operators, "*"),
                        Node(NonTerminalSymbol.PointerOperand,
                            Node(NonTerminalSymbol.SimpleExpression,
                                Leaf(LexicalGrammarCategory.BracesAndParentheses, "("),
                                Node(NonTerminalSymbol.OpenExpression,
                                    Node(NonTerminalSymbol.LogicalOperand,
                                        Node(NonTerminalSymbol.ComparisonOperand,
                                            Node(NonTerminalSymbol.ArithmeticOperand,
                                                Leaf(LexicalGrammarCategory.Operators, "*"),
                                                Leaf(LexicalGrammarCategory.Operators, "*"),
                                                Node(NonTerminalSymbol.PointerOperand,
                                                    Node(NonTerminalSymbol.SimpleExpression,
                                                        Leaf(LexicalGrammarCategory.VariableIdentifiers, "y")))),
                                            Leaf(LexicalGrammarCategory.Operators, "+"),
                                            Node(NonTerminalSymbol.ArithmeticOperand,
                                                Node(NonTerminalSymbol.PointerOperand,
                                                    Node(NonTerminalSymbol.SimpleExpression,
                                                        Node(NonTerminalSymbol.LiteralValue,
                                                            Leaf(LexicalGrammarCategory.Literals, "2")))))))),
                                Leaf(LexicalGrammarCategory.BracesAndParentheses, ")"))))))
        ).Convert();

        Assert.True(AstNode.From(input) is PointerDereference
        {
            Pointer: Infix
            {
                Left: PointerDereference
                {
                    Pointer: PointerDereference
                    {
                        Pointer: VariableValue
                        {
                            Identifier.Name: "y"
                        }
                    }
                },
                Operator: Infix.Op.Plus,
                Right: IntLiteralValue
                {
                    Value: 2
                }
            }
        });
    }

    [Fact]
    public void FieldAccess_Conversion()
    {
        // *x.field
        var input = Node(NonTerminalSymbol.OpenExpression,
            Node(NonTerminalSymbol.LogicalOperand,
                Node(NonTerminalSymbol.ComparisonOperand,
                    Node(NonTerminalSymbol.ArithmeticOperand,
                        Leaf(LexicalGrammarCategory.Operators, "*"),
                        Node(NonTerminalSymbol.PointerOperand,
                            Node(NonTerminalSymbol.SimpleExpression,
                                Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")),
                            Leaf(LexicalGrammarCategory.Operators, "."),
                            Leaf(LexicalGrammarCategory.VariableIdentifiers, "field")))))
        ).Convert();

        Assert.True(AstNode.From(input) is PointerDereference
        {
            Pointer: StructFieldAccess
            {
                FieldName.Name: "field",
                Left: VariableValue
                {
                    Identifier.Name: "x"
                }
            }
        });
    }

    [Fact]
    public void AssignmentPointer_Conversion()
    {
        // *x = 5
        var input = Node(NonTerminalSymbol.OpenExpression,
            Node(NonTerminalSymbol.AssignmentOperand,
                Node(NonTerminalSymbol.LogicalOperand,
                    Node(NonTerminalSymbol.ComparisonOperand,
                        Node(NonTerminalSymbol.ArithmeticOperand,
                            Leaf(LexicalGrammarCategory.Operators, "*"),
                            Node(NonTerminalSymbol.PointerOperand,
                                Node(NonTerminalSymbol.SimpleExpression,
                                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "x"))))))),
            Leaf(LexicalGrammarCategory.Operators, "="),
            Node(NonTerminalSymbol.AssignmentOperand,
                Node(NonTerminalSymbol.LogicalOperand,
                    Node(NonTerminalSymbol.ComparisonOperand,
                        Node(NonTerminalSymbol.ArithmeticOperand,
                            Node(NonTerminalSymbol.PointerOperand,
                                Node(NonTerminalSymbol.SimpleExpression,
                                    Node(NonTerminalSymbol.LiteralValue,
                                        Leaf(LexicalGrammarCategory.Literals, "5"))))))))
        ).Convert();

        Assert.True(AstNode.From(input) is Assignment
        {
            Left: PointerDereference
            {
                Pointer: VariableValue
                {
                    Identifier.Name: "x"
                }
            },
            Right: IntLiteralValue
            {
                Value: 5
            }
        });
    }

    [Fact]
    public void AssignmentField_Conversion()
    {
        // x.field = 5
        var input = Node(NonTerminalSymbol.OpenExpression,
            Node(NonTerminalSymbol.AssignmentOperand,
                Node(NonTerminalSymbol.LogicalOperand,
                    Node(NonTerminalSymbol.ComparisonOperand,
                        Node(NonTerminalSymbol.ArithmeticOperand,
                            Node(NonTerminalSymbol.PointerOperand,
                                Node(NonTerminalSymbol.SimpleExpression,
                                    Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")),
                                Leaf(LexicalGrammarCategory.Operators, "."),
                                Leaf(LexicalGrammarCategory.VariableIdentifiers, "field")))))),
            Leaf(LexicalGrammarCategory.Operators, "="),
            Node(NonTerminalSymbol.AssignmentOperand,
                Node(NonTerminalSymbol.LogicalOperand,
                    Node(NonTerminalSymbol.ComparisonOperand,
                        Node(NonTerminalSymbol.ArithmeticOperand,
                            Node(NonTerminalSymbol.PointerOperand,
                                Node(NonTerminalSymbol.SimpleExpression,
                                    Node(NonTerminalSymbol.LiteralValue,
                                        Leaf(LexicalGrammarCategory.Literals, "5"))))))))
        ).Convert();

        Assert.True(AstNode.From(input) is Assignment
        {
            Left: StructFieldAccess
            {
                Left: VariableValue
                {
                    Identifier.Name: "x"
                },
                FieldName.Name: "field"
            },
            Right: IntLiteralValue
            {
                Value: 5
            }
        });
    }
}
