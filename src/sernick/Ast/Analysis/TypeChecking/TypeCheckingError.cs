namespace sernick.Ast.Analysis.TypeChecking;
using sernick.Utility;

using sernick.Diagnostics;
using sernick.Input;
using sernick.Ast.Nodes;

public abstract record TypeCheckingErrorBase() : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}

public sealed record TypesMismatchError(Type Required, Type Provided, ILocation Location)
    : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Type checking error: required \"{Required}\", provided \"{Provided}\" at {Location}";
    }
}

public sealed record NotAStructTypeError(Type Provided, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Type is not a struct type: \"{Provided}\" at {Location}";
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

public sealed record FieldNotPresentInStructError(Type Struct, Identifier Field, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Struct \"{Struct}\" does not contain field \"{Field.Name}\" provided at {Location}";
    }
}

public sealed record MissingFieldInitialization(Type Struct, string Field, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString() => $"Struct \"{Struct}\" is missing field \"{Field}\" at {Location}";
}

public sealed record DuplicateFieldInitialization(string Field, Range<ILocation> First, Range<ILocation> Second) : TypeCheckingErrorBase
{
    public override string ToString() =>
        $"Field \"{Field}\" at: {Second.Start}, {Second.End}, is declared earlier at: {First.Start}, {First.End}.";
}

public sealed record CannotDereferenceExpressionError(Type InferredExpressionType, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Attempted to dereference expression of type \"{InferredExpressionType}\" which is not of pointer type, at: {Location}";
    }
}

public sealed record CannotAutoDereferenceNotAStructPointer(PointerType PointerType, ILocation Location) : TypeCheckingErrorBase
{
    public override string ToString()
    {
        return $"Pointer of type \"${PointerType}\" cannot be auto-dereferenced as it is not a struct pointer, at: {Location}";
    }
}

public sealed record RecursiveStructDeclaration(FieldDeclaration Field) : TypeCheckingErrorBase
{
    // public override string ToString() =>
    //     $"Field \"{Field.Name.Name}\" can't have the same type \"{Field.Type}\" as the parent struct, at {Field.LocationRange.Start}, {Field.LocationRange.End}";
}

public sealed record DuplicateFieldDeclaration(FieldDeclaration First, FieldDeclaration Second) : TypeCheckingErrorBase
{
    public override string ToString() =>
        $"Field with name \"{First.Name.Name}\" at: {Second.LocationRange.Start}, {Second.LocationRange.End}, is declared earlier at {First.LocationRange.Start}, {First.LocationRange.End}";
}
