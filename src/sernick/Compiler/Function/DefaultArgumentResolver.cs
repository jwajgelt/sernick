namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public static class DefaultArgumentResolver
{
    public static CodeTreeValueNode GetDefaultValue(this IFunctionParam param)
    {
        return param.TryGetDefaultValue() switch
        {
            null => throw new Exception("Requested a default value, but there is none."),
            int intValue => intValue,
            bool boolValue => boolValue ? 1 : 0,
            _ => throw new Exception("Default value of unsupported type."),
        };
    }
}
