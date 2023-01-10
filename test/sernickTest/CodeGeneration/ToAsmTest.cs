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
        var dict = new Dictionary<Register, HardwareRegister>();
        var label = new Label("labelString");

        var asm = label.ToAsm(dict);

        Assert.Equal("labelString:", asm);
    }

    [Fact]
    public void RegInstructionOperandToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>(null, null, null, null, null);
        hardwareReg.Setup(reg => reg.ToString()).Returns("RAX");
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };
        var regOp = new RegInstructionOperand(virtualReg.Object);

        var asm = regOp.ToAsm(dict);

        Assert.Equal("rax", asm);
    }

    [Fact]
    public void MemInstructionOperandFromRegToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>(null, null, null, null, null);
        hardwareReg.Setup(reg => reg.ToString()).Returns("RAX");
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };
        var memOp = virtualReg.Object.AsMemOperand();

        var asm = memOp.ToAsm(dict);

        Assert.Equal("[rax]", asm);
    }

    [Fact]
    public void MemInstructionOperandFromRegAndDisplacementToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>(null, null, null, null, null);
        hardwareReg.Setup(reg => reg.ToString()).Returns("RAX");
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };
        var displacement = new RegisterValue(126);
        var memOp = new MemInstructionOperand(virtualReg.Object, null, (isNegative: false, displacement));

        var asm = memOp.ToAsm(dict);

        Assert.Equal("[rax + 126]", asm);
    }

    [Fact]
    public void MemInstructionOperandFromRegAndNegativeDisplacementToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>(null, null, null, null, null);
        hardwareReg.Setup(reg => reg.ToString()).Returns("RAX");
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };
        var displacement = new RegisterValue(-979);
        var memOp = new MemInstructionOperand(virtualReg.Object, null, (isNegative: false, displacement));

        var asm = memOp.ToAsm(dict);

        Assert.Equal("[rax - 979]", asm);
    }

    [Fact]
    public void MemInstructionOperandFromRegAndNegatedDisplacementToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>(null, null, null, null, null);
        hardwareReg.Setup(reg => reg.ToString()).Returns("RAX");
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };
        var displacement = new RegisterValue(979);
        var memOp = new MemInstructionOperand(virtualReg.Object, null, (isNegative: true, displacement));

        var asm = memOp.ToAsm(dict);

        Assert.Equal("[rax - 979]", asm);
    }

    [Fact]
    public void MemInstructionOperandFromLabelAndDisplacementToAsm()
    {
        var label = new Label("base");
        var displacement = new RegisterValue(96);
        var dict = new Dictionary<Register, HardwareRegister>();
        var memOp = (label, (isNegative: false, displacement)).AsMemOperand();

        var asm = memOp.ToAsm(dict);

        Assert.Equal("[base + 96]", asm);
    }

    [Fact]
    public void ImmInstructionOperandToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var immOp = new ImmInstructionOperand(new RegisterValue(7321));

        var asm = immOp.ToAsm(dict);

        Assert.Equal("7321", asm);
    }

    [Fact]
    public void BinaryAssignInstructionToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var binAssign = new BinaryAssignInstruction(BinaryAssignInstructionOp.Add, PrepareOperand("left"), PrepareOperand("right"));

        var asm = binAssign.ToAsm(dict);

        Assert.Equal("\tadd\tleft, right", asm);
    }

    [Fact]
    public void BinaryComputeInstructionToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var binComp = new BinaryComputeInstruction(BinaryComputeInstructionOp.Cmp, PrepareOperand("left"), PrepareOperand("right"));

        var asm = binComp.ToAsm(dict);

        Assert.Equal("\tcmp\tleft, right", asm);
    }

    [Fact]
    public void MovToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var mov = new MovInstruction(PrepareOperand("left"), PrepareOperand("right"));

        var asm = mov.ToAsm(dict);

        Assert.Equal("\tmov\tleft, right", asm);

    }

    [Fact]
    public void UnaryToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var un = new UnaryOpInstruction(UnaryOp.Neg, PrepareOperand("operand"));

        var asm = un.ToAsm(dict);

        Assert.Equal("\tneg\toperand", asm);
    }

    [Fact]
    public void JmpCcToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var jmpNo = new JmpCcInstruction(ConditionCode.No, "target");

        var asm = jmpNo.ToAsm(dict);

        Assert.Equal("\tjno\ttarget", asm);
    }

    [Fact]
    public void CallToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var call = new CallInstruction("target");

        var asm = call.ToAsm(dict);

        Assert.Equal("\tcall\ttarget", asm);
    }

    [Fact]
    public void JmpToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var jmp = new JmpInstruction("target");

        var asm = jmp.ToAsm(dict);

        Assert.Equal("\tjmp\ttarget", asm);
    }

    [Fact]
    public void RetToAsm()
    {
        var dict = new Dictionary<Register, HardwareRegister>();
        var ret = new RetInstruction();

        var asm = ret.ToAsm(dict);

        Assert.Equal("\tret", asm);
    }

    [Fact]
    public void SetCcToAsm()
    {
        var virtualReg = new Mock<Register>();
        var hardwareReg = new Mock<HardwareRegister>("RAX", "rax", "eax", "ax", "al");
        var dict = new Dictionary<Register, HardwareRegister> { [virtualReg.Object] = hardwareReg.Object };
        var setNo = new SetCcInstruction(ConditionCode.No, virtualReg.Object);

        var asm = setNo.ToAsm(dict);

        Assert.Equal("\tsetno\tal", asm);
    }

    private static IInstructionOperand PrepareOperand(string toAsm)
    {
        var op = new Mock<IInstructionOperand>();
        op.Setup(o => o.ToAsm(It.IsAny<IReadOnlyDictionary<Register, HardwareRegister>>())).Returns(toAsm);
        return op.Object;
    }
}
