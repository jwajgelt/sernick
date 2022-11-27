namespace sernickTest.Compiler.Function;

using sernick.Ast;
using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;
using static sernickTest.Ast.Helpers.AstNodesExtensions;

public class FunctionContextGenerateVariableAccessTest
{
    [Fact]
    public void Generates_RegisterRead_for_exclusive_local_Variable()
    {
        var context = new FunctionContext(null, Array.Empty<IFunctionParam>(), false, 0);
        var variable = Var("x");
        context.AddLocal(variable, usedElsewhere: false);

        var readCodeTree = context.GenerateVariableRead(variable);

        Assert.IsType<RegisterRead>(readCodeTree);
    }

    [Fact]
    public void Generates_RegisterWrite_for_exclusive_local_Variable()
    {
        var context = new FunctionContext(null, Array.Empty<IFunctionParam>(), false, 0);
        var variable = Var("x");
        context.AddLocal(variable, usedElsewhere: false);
        var value = new Constant(new RegisterValue(1));

        var writeCodeTree = context.GenerateVariableWrite(variable, value);

        var registerWrite = Assert.IsType<RegisterWrite>(writeCodeTree);
        Assert.Equal(value, registerWrite.Value);
    }

    [Fact]
    public void Generates_MemoryRead_for_shared_Variable()
    {
        var context = new FunctionContext(null, Array.Empty<IFunctionParam>(), false, 0);
        var variable = Var("x");
        context.AddLocal(variable, usedElsewhere: true);

        var readCodeTree = context.GenerateVariableRead(variable);

        var expectedTree =
            new MemoryRead(
                new BinaryOperationNode(
                    BinaryOperation.Sub,
                    new RegisterRead(HardwareRegister.RBP),
                    new Constant(new RegisterValue(8)))
                );
        Assert.Equal(expectedTree, readCodeTree);
    }

    [Fact]
    public void Generates_MemoryRead_for_FunctionParams()
    {
        Fun<UnitType>("foo").Parameter<IntType>("arg", out var arg);
        var context = new FunctionContext(null, new[] { arg }, false, 0);

        var readCodeTree = context.GenerateVariableRead(arg);

        var expectedTree =
            new MemoryRead(
                new BinaryOperationNode(
                    BinaryOperation.Sub,
                    new RegisterRead(HardwareRegister.RBP),
                    new Constant(new RegisterValue(-16)))
            );
        Assert.Equal(expectedTree, readCodeTree);
    }

    [Fact]
    public void Generates_MemoryWrite_for_shared_Variable()
    {
        var context = new FunctionContext(null, Array.Empty<IFunctionParam>(), false, 0);
        var variable = Var("x");
        context.AddLocal(variable, usedElsewhere: true);
        var value = new Constant(new RegisterValue(1));

        var readCodeTree = context.GenerateVariableWrite(variable, value);

        var expectedTree =
            new MemoryWrite(
                new BinaryOperationNode(
                    BinaryOperation.Sub,
                    new RegisterRead(HardwareRegister.RBP),
                    new Constant(new RegisterValue(8))),
                value
            );
        Assert.Equal(expectedTree, readCodeTree);
    }

    [Fact]
    public void Throws_when_Variable_is_Undefined()
    {
        var varX = Var("x");
        var varY = Var("y");
        var undefinedVar = Var("undefined");
        var value = new Constant(new RegisterValue(1));

        var parentContext = new FunctionContext(null, Array.Empty<IFunctionParam>(), false, 0);
        var context = new FunctionContext(parentContext, Array.Empty<IFunctionParam>(), false, 1);
        parentContext.AddLocal(varX, true);
        context.AddLocal(varX, true);
        context.AddLocal(varY, false);

        Assert.Throws<ArgumentException>(() => context.GenerateVariableRead(undefinedVar));
        Assert.Throws<ArgumentException>(() => context.GenerateVariableWrite(undefinedVar, value));
    }

    [Fact]
    public void Generates_Indirect_MemoryRead_outer_Variable()
    {
        var variable = Var("x");
        var displayAddress = new Constant(new RegisterValue(100));

        var parentContext = new FunctionContext(null, Array.Empty<IFunctionParam>(), false, 0);
        parentContext.AddLocal(variable, true);
        parentContext.SetDisplayAddress(displayAddress);
        var context = new FunctionContext(parentContext, Array.Empty<IFunctionParam>(), false, 1);

        var readCodeTree = context.GenerateVariableRead(variable);

        var expectedTree = new MemoryRead(
            new BinaryOperationNode(
                BinaryOperation.Sub,
                new MemoryRead(
                    new BinaryOperationNode(
                        BinaryOperation.Add,
                        displayAddress,
                        new Constant(new RegisterValue(0)))
                    ),
                new Constant(new RegisterValue(8))
                )
            );
        Assert.Equal(expectedTree, readCodeTree);
    }

    [Fact]
    public void Generates_Indirect_MemoryWrite_outer_Variable()
    {
        var variable = Var("x");
        var value = new Constant(new RegisterValue(1));
        var displayAddress = new Constant(new RegisterValue(100));

        var parentContext = new FunctionContext(null, Array.Empty<IFunctionParam>(), false, 0);
        parentContext.AddLocal(variable, true);
        parentContext.SetDisplayAddress(displayAddress);
        var context = new FunctionContext(parentContext, Array.Empty<IFunctionParam>(), false, 1);

        var readCodeTree = context.GenerateVariableWrite(variable, value);

        var expectedTree = new MemoryWrite(
            new BinaryOperationNode(
                BinaryOperation.Sub,
                new MemoryRead(
                    new BinaryOperationNode(
                        BinaryOperation.Add,
                        displayAddress,
                        new Constant(new RegisterValue(0)))
                ),
                new Constant(new RegisterValue(8))
            ),
            value
        );
        Assert.Equal(expectedTree, readCodeTree);
    }
}
