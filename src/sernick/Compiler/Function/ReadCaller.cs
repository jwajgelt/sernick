namespace sernick.Compiler.Function;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using static Compiler.PlatformConstants;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Convention;
using static Helpers;

public sealed class ReadCaller : IFunctionCaller
{
    // Not 100 % sure about it, but it should map to "%d\0"
    private const int FORMAT_STRING = 0x256400;
    public Label Label { get; } = "scanf";

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        if (arguments.Count != 0)
        {
            throw new Exception("Read should not take any arguments.");
        }

        var operations = new List<CodeTreeNode>();

        // Caller-saved registers
        var callerSavedMap = new Dictionary<HardwareRegister, Register>(ReferenceEqualityComparer.Instance);
        foreach (var reg in CallerToSave)
        {
            var tempReg = new Register();
            callerSavedMap[reg] = tempReg;
            operations.Add(Reg(tempReg).Write(Reg(reg).Read()));
        }

        Register rsp = HardwareRegister.RSP;
        var rspRead = Reg(rsp).Read();
        var pushRsp = Reg(rsp).Write(rspRead - POINTER_SIZE);

        // Allocate slot for input value
        operations.Add(pushRsp);

        // Arguments:
        // address
        operations.Add(pushRsp);
        operations.Add(Mem(rspRead).Write(rspRead + POINTER_SIZE));

        // format string
        operations.Add(pushRsp);
        operations.Add(Mem(rspRead).Write(FORMAT_STRING));

        // Performing actual call (puts return addess on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Remove arguments from stack (we already returned from call)
        operations.Add(Reg(rsp).Write(rspRead + POINTER_SIZE * 2));

        // Put value from stack to virtual register
        var returnValueRegister = new Register();
        operations.Add(Reg(returnValueRegister).Write(Mem(rspRead).Read()));
        CodeTreeValueNode returnValueLocation = Reg(returnValueRegister).Read();

        // Free stack output slot
        operations.Add(Reg(rsp).Write(rspRead + POINTER_SIZE));

        // Retrieve values of caller-saved registers
        foreach (var reg in CallerToSave)
        {
            var tempReg = callerSavedMap[reg];
            var tempVal = Reg(tempReg).Read();
            operations.Add(Reg(reg).Write(tempVal));
        }

        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations), returnValueLocation);
    }
}
