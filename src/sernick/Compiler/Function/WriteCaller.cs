namespace sernick.Compiler.Function;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using static Compiler.PlatformConstants;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Convention;

public sealed class WriteCaller : IFunctionCaller
{
    private const int FORMAT_STRING = 0xa6425;
    public Label Label { get; } = "printf";

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
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

        // Put format string onto stack
        operations.Add(pushRsp);
        operations.Add(Mem(rspRead).Write(FORMAT_STRING));

        // Arguments:
        // format string
        operations.Add(Reg(HardwareRegister.RDI).Write(rspRead));
        // value to print
        operations.Add(Reg(HardwareRegister.RSI).Write(arguments.Single()));

        // Align the stack
        var tmpRsp = Reg(new Register());
        operations.Add(tmpRsp.Write(rspRead));
        operations.Add(Reg(rsp).Write(rspRead & -2 * POINTER_SIZE));

        // Performing actual call (puts return addess on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Restore stack pointer
        operations.Add(Reg(rsp).Write(tmpRsp.Read()));

        // Free format string slot
        operations.Add(Reg(rsp).Write(rspRead + POINTER_SIZE));

        // Retrieve values of caller-saved registers
        foreach (var reg in CallerToSave)
        {
            var tempReg = callerSavedMap[reg];
            var tempVal = Reg(tempReg).Read();
            operations.Add(Reg(reg).Write(tempVal));
        }

        return new IFunctionCaller.GenerateCallResult(operations, null);
    }
}
