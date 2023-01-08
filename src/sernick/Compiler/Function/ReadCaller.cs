namespace sernick.Compiler.Function;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using static Compiler.PlatformConstants;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Helpers;

public sealed class ReadCaller : IFunctionCaller
{
    private const int FORMAT_STRING = 0x6425;
    public Label Label { get; } = "scanf";

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        if (arguments.Count != 0)
        {
            throw new Exception("Read should not take any arguments.");
        }

        var operations = new List<CodeTreeNode>();

        Register rsp = HardwareRegister.RSP;
        var rspRead = Reg(rsp).Read();
        var pushRsp = Reg(rsp).Write(rspRead - POINTER_SIZE);

        // Put format string onto stack
        operations.Add(pushRsp);
        operations.Add(Mem(rspRead).Write(FORMAT_STRING));

        // Allocate slot for input value
        operations.Add(pushRsp);
        operations.Add(Mem(rspRead).Write(0));

        // Arguments:
        // format string's address
        operations.Add(Reg(HardwareRegister.RDI).Write(rspRead + POINTER_SIZE));
        // address
        operations.Add(Reg(HardwareRegister.RSI).Write(rspRead));

        // Align the stack
        var tmpRsp = Reg(new Register());
        operations.Add(tmpRsp.Write(rspRead));
        operations.Add(Reg(rsp).Write(rspRead & -2 * POINTER_SIZE));

        // Performing actual call (puts return address on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Restore stack pointer
        operations.Add(Reg(rsp).Write(tmpRsp.Read()));

        // Put value from stack to virtual register
        var returnValueRegister = new Register();
        operations.Add(Reg(returnValueRegister).Write(Mem(rspRead).Read()));
        CodeTreeValueNode returnValueLocation = Reg(returnValueRegister).Read();

        // Free stack output slot + format string slot
        operations.Add(Reg(rsp).Write(rspRead + 2 * POINTER_SIZE));

        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations), returnValueLocation);
    }
}
