namespace sernickTest.CodeGeneration.RegisterAllocator;

using ControlFlowGraph;
using Moq;
using sernick.CodeGeneration;
using sernick.CodeGeneration.RegisterAllocation;
using sernick.Compiler.Function;
using sernick.Compiler.Instruction;
using sernick.ControlFlowGraph.Analysis;
using sernick.ControlFlowGraph.CodeTree;
using static sernick.ControlFlowGraph.CodeTree.CodeTreeExtensions;

public class SpillsAllocatorTest
{
    [Fact]
    public void Handles_spilled_MovInstruction()
    {
        var fromRegister = new Register();
        var locationRegister = new Register();
        var instruction = MovInstruction.ToReg(fromRegister).FromMem(locationRegister);
        
        var variableLocation = new Mock<VariableLocation>();
        var variableReference = Mem(Reg(HardwareRegister.RBP).Read());
        variableLocation.Setup(vl => vl.GenerateRead())
            .Returns(variableReference.Read);
        variableLocation.Setup(vl => vl.GenerateWrite(It.IsAny<RegisterRead>()))
            .Returns((CodeTreeValueNode valueNode) => variableReference.Write(valueNode));
        
        var functionContext = new Mock<IFunctionContext>();
        functionContext.Setup(fc => fc.AllocateStackFrameSlot()).Returns(variableLocation.Object);

        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        var reservedRegister = (FakeHardwareRegister)"A";
        
        var spillsAllocator = new SpillsAllocator(new [] { reservedRegister }, covering);
        var incompleteAllocation = new Dictionary<Register, HardwareRegister?>
        {
            { fromRegister, null }, { locationRegister, null }
        };

        var asm = new IAsmable[] { instruction };
        var (newAsm, _) = spillsAllocator.Process(asm, functionContext.Object, incompleteAllocation);
        
        Assert.Equal(3, newAsm.Count);
        
        // First instruction is Mov from memory to the reserved register.
        var readInstruction = Assert.IsType<MovInstruction>(newAsm[0]);
        var readTarget = Assert.IsType<RegInstructionOperand>(readInstruction.Target);
        Assert.Equal(reservedRegister, readTarget.Register);
        
        // Second instruction is input instruction with replaced registers.
        Assert.Equal(MovInstruction.ToReg(reservedRegister).FromMem(reservedRegister), newAsm[1]);

        // Third instruction is Mov from reserved register to memory.
        var writeInstruction = Assert.IsType<MovInstruction>(newAsm[2]);
        var writeSource = Assert.IsType<RegInstructionOperand>(writeInstruction.Source);
        Assert.Equal(reservedRegister, writeSource.Register);
    }

    [Fact]
    public void Ignores_Allocated_Instructions()
    {
        var fromRegister = new Register();
        var locationRegister = new Register();
        var instruction = MovInstruction.ToReg(fromRegister).FromMem(locationRegister);
        var covering = new InstructionCovering(SernickInstructionSet.Rules);
        
        var functionContext = new Mock<IFunctionContext>();
        
        var spillsAllocator = new SpillsAllocator(Array.Empty<HardwareRegister>(), covering);
        
        var completeAllocation = new Dictionary<Register, HardwareRegister?>
        {
            { fromRegister, (FakeHardwareRegister)"A" },
            { locationRegister, (FakeHardwareRegister)"B" }
        };
        var asm = new IAsmable[] { instruction };
        var (newAsm, _) = spillsAllocator.Process(asm, functionContext.Object, completeAllocation);

        Assert.Single(newAsm, instruction);
    }
}
