namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public static class DefaultArgumentResolver
{
    public static CodeTreeValueNode GetDefaultValue(IFunctionParam param)
    {
        var defaultValue = param.TryGetDefaultValue();
        if (defaultValue == null)
        {
            throw new Exception("Requested a default value, but there is none.");
        }

        if (defaultValue is int intValue)
        {
            return intValue;
        }

        if (defaultValue is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        throw new Exception("Default value of unsupported type.");
    }
}
