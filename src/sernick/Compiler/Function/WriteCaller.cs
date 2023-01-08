namespace sernick.Compiler.Function;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using static Compiler.PlatformConstants;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static Helpers;

public sealed class WriteCaller : IFunctionCaller
{
    private const int FORMAT_STRING = 0xa6425;
    public Label Label { get; } = "printf";

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        var operations = new List<CodeTreeNode>();

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

        // Performing actual call (puts return address on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Restore stack pointer
        operations.Add(Reg(rsp).Write(tmpRsp.Read()));

        // Free format string slot
        operations.Add(Reg(rsp).Write(rspRead + POINTER_SIZE));

        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations), null);
    }
}
