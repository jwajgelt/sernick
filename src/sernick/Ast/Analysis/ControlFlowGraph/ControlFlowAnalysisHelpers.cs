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
    
    public class DereferencedLocation : IValueLocation
    {
        private readonly IValueLocation _location;

        public DereferencedLocation(IValueLocation location)
        {
            _location = location;
        }

        public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value)
        {
            // StructValueLocation is a pointer to the first field in a struct.
            // No need to do a dereference in this case.
            if (_location is StructValueLocation)
            {
                return _location.GenerateValueWrite(value);
            }
            return new MemoryWrite(_location.GenerateValueRead(), value).Enumerate();
        }

        public CodeTreeValueNode GenerateValueRead()
        {
            // StructValueLocation is a pointer to the first field in a struct.
            // No need to do a dereference in this case.
            if (_location is StructValueLocation)
            {
                return _location.GenerateValueRead();
            }
            return new MemoryRead(_location.GenerateValueRead());
        }
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
            private readonly IValueLocation _location;

            public PrimitiveFieldLocation(IValueLocation location, int offset)
            {
                _offset = offset;
                _location = location;
            }

            public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value)
            {
                return CodeTreeExtensions.Mem(StackLocationAddress).Write(value).Enumerate();
            }

            public CodeTreeValueNode GenerateValueRead()
            {
                return CodeTreeExtensions.Mem(StackLocationAddress).Read();
            }

            private CodeTreeValueNode StackLocationAddress => _location.GenerateValueRead() + _offset;
        }

        private readonly int _size;
        private readonly int _offset;
        private readonly IValueLocation _location;

        public StructValueLocation(IValueLocation location, int size)
        {
            _location = location;
            _size = size;
            _offset = 0;
        }

        private StructValueLocation(IValueLocation location, int size, int offset)
        {
            _location = location;
            _size = size;
            _offset = offset;
        }

        public IValueLocation GetPrimitiveField(int fieldOffset)
        {
            if (fieldOffset + PlatformConstants.POINTER_SIZE > _size)
            {
                throw new NotSupportedException();
            }

            return new PrimitiveFieldLocation(_location, _offset + fieldOffset);
        }

        public IStructValueLocation GetField(int fieldOffset, int fieldSize)
        {
            if (fieldOffset + fieldSize > _size)
            {
                throw new NotSupportedException();
            }

            return new StructValueLocation(_location, fieldSize, _offset + fieldOffset);
        }

        public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value)
        {
            return StructHelper.GenerateStructCopy(StackLocationAddress, value, _size);
        }

        public CodeTreeValueNode GenerateValueRead()
        {
            return StackLocationAddress;
        }

        private CodeTreeValueNode StackLocationAddress => _location.GenerateValueRead() + _offset;
    }
}
