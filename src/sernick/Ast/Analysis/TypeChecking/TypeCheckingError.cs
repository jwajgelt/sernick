namespace sernick.Ast.Analysis.TypeChecking;

using Diagnostics;
using Input;
using sernick.Ast.Nodes;

public abstract record TypeCheckingErrorBase() : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}

public sealed record TypesMismatchError(Type Required, Type Provided, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Type checking error: required \"{Required}\", provided \"{Provided}\" at {Location}";
    }
}

public sealed record InferredBadFunctionReturnType(Type Declared, Type Inferred, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Inferred function return type is not equal to declared return type: declared \"{Declared}\", inferred \"{Inferred}\" at {Location}";
    }
}

public sealed record WrongNumberOfFunctionArgumentsError(int Expected, int Actual, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Wrong number of arguments in a function call: expected at least \"{Expected}\", provided \"{Actual}\" at {Location}";
    }
}

public sealed record InfixOperatorTypeError(Infix.Op Operator, Type LhsType, Type RhsType, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Cannot apply an infix operator ${Operator} with types: ${LhsType} and ${RhsType}: at {Location}";
    }
}

public sealed record ReturnTypeError(Type Required, Type Provided, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Return expression has bad type: required \"{Required}\", provided \"{Provided}\" at {Location}";
    }
}

public sealed record TypeOrInitialValueShouldBePresentError(ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Type or initial variable value have to be present. Found none at {Location}";
    }
}

public sealed record UnequalBranchTypeError(Type TrueBranchType, Type FalseBranchType, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"True/false branch in If statement should have the same type, but they do not: true branch of type \"{TrueBranchType}\", false branch of type \"{FalseBranchType}\" at {Location}";
    }
}

public sealed record UnitTypeNotAllowedInFunctionArgumentError(ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Unit type is not allowed as a function argument type: at {Location}";
    }
}

public sealed record WrongFunctionArgumentError(Type Required, Type Provided, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Wrong function argument type: expected \"{Required}\", provided \"{Provided}\" at {Location}";
    }
}

public sealed record UnhandledOperatorError(Infix.Op Operator, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Type checking cannot handle an operator \"{Operator}\", at {Location}";
    }
}
