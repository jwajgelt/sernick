namespace sernickTest.CodeGeneration;

using Moq;
using sernick.CodeGeneration;
using sernick.Compiler.Instruction;
using sernick.ControlFlowGraph.CodeTree;

public class ToAsmTest
{
    [Fact]
    public void LabelToAsm()
    {
        var label = new Label("labelString");
        var dict = new Dictionary<Register, HardwareRegister>();

        var asm = label.ToAsm(dict);
        
        Assert.Equal("labelString:", asm);
    }

    [Fact]
    public void RegInstructionOperandToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>(null);
        hardwareReg.Setup(reg => reg.ToString()).Returns("RAX");
        var regOp = new RegInstructionOperand(virtualReg.Object);
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };

        var asm = regOp.ToAsm(dict);
        
        Assert.Equal("rax", asm);
    }
    
    [Fact]
    public void MemInstructionOperandFromRegToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>(null);
        hardwareReg.Setup(reg => reg.ToString()).Returns("RAX");
        var memOp = virtualReg.Object.AsMemOperand();
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };

        var asm = memOp.ToAsm(dict);
        
        Assert.Equal("[rax]", asm);
    }
    
    [Fact]
    public void MemInstructionOperandFromLabelAndDisplacementToAsm()
    {
        var label = new Label("base");
        var displacement = new RegisterValue(96);
        var memOp = (label, displacement).AsMemOperand();
        var dict = new Dictionary<Register, HardwareRegister>();

        var asm = memOp.ToAsm(dict);
        
        Assert.Equal("[base + 96]", asm);
    }
    
    [Fact]
    public void ImmInstructionOperandToAsm()
    {
        var immOp = new ImmInstructionOperand(new RegisterValue(7321));
        var dict = new Dictionary<Register, HardwareRegister>();
        
        var asm = immOp.ToAsm(dict);
        
        Assert.Equal("7321", asm);
    }
    
    
    
    
}
