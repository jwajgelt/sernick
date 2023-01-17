namespace sernick.Ast.Analysis.ControlFlowGraph;

using Compiler;
using Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;
using Utility;

public static class ControlFlowAnalysisHelpers
{
    public interface IValueLocation
    {
        public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value);

        public CodeTreeValueNode GenerateValueRead();
    }

    public interface IStructValueLocation : IValueLocation
    {
        public IValueLocation GetPrimitiveField(int fieldOffset);
        public IStructValueLocation GetField(int fieldOffset, int fieldSize);
    }

    public class VariableValueLocation : IValueLocation
    {
        private readonly IFunctionContext _functionContext;
        private readonly IFunctionVariable _variable;

        public VariableValueLocation(IFunctionContext functionContext, IFunctionVariable variable)
        {
            _functionContext = functionContext;
            _variable = variable;
        }

        public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value) => _functionContext.GenerateVariableWrite(_variable, value).Enumerate();

        public CodeTreeValueNode GenerateValueRead()
        {
            return _functionContext.GenerateVariableRead(_variable);
        }
    }

    public class StructValueLocation : IStructValueLocation
    {
        private class PrimitiveFieldLocation : IValueLocation
        {
            private readonly int _offset;
            private readonly IFunctionContext _functionContext;
            private readonly IFunctionVariable _temp;

            public PrimitiveFieldLocation(IFunctionContext functionContext, IFunctionVariable temp, int offset)
            {
                _offset = offset;
                _functionContext = functionContext;
                _temp = temp;
            }

            public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value)
            {
                return CodeTreeExtensions.Mem(StackLocationAddress).Write(value).Enumerate();
            }

            public CodeTreeValueNode GenerateValueRead()
            {
                return CodeTreeExtensions.Mem(StackLocationAddress).Read();
            }

            private CodeTreeValueNode StackLocationAddress => _functionContext.GenerateVariableRead(_temp) + _offset;
        }

        private readonly IFunctionContext _functionContext;
        private readonly int _size;
        private readonly int _offset;
        private readonly IFunctionVariable _temp;

        public StructValueLocation(IFunctionContext functionContext, IFunctionVariable temp, int size)
        {
            _functionContext = functionContext;
            _size = size;
            _offset = 0;
            _temp = temp;
        }

        private StructValueLocation(IFunctionContext functionContext, IFunctionVariable temp, int size, int offset)
        {
            _functionContext = functionContext;
            _temp = temp;
            _size = size;
            _offset = offset;
        }

        public IValueLocation GetPrimitiveField(int fieldOffset)
        {
            if (fieldOffset + PlatformConstants.POINTER_SIZE > _size)
            {
                throw new NotSupportedException();
            }

            return new PrimitiveFieldLocation(_functionContext, _temp, _offset + fieldOffset);
        }

        public IStructValueLocation GetField(int fieldOffset, int fieldSize)
        {
            if (fieldOffset + fieldSize > _size)
            {
                throw new NotSupportedException();
            }

            return new StructValueLocation(_functionContext, _temp, fieldSize, _offset + fieldOffset);
        }

        public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value)
        {
            return StructHelper.GenerateStructCopy(StackLocationAddress, value, _size);
        }

        public CodeTreeValueNode GenerateValueRead()
        {
            return StackLocationAddress;
        }

        private CodeTreeValueNode StackLocationAddress => _functionContext.GenerateVariableRead(_temp) + _offset;
    }
}
