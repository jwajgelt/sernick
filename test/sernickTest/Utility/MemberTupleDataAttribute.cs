namespace sernickTest.Utility;

using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

/// <summary>
/// Provides a data source for a data theory, with the data coming from one of the following sources:
/// 1. A static property
/// 2. A static field
/// 3. A static method (with parameters)
/// The member must return something compatible with IEnumerable&lt;ITuple&gt; with the test data.
/// Caution: the property is completely enumerated by .ToList() before any test is run. Hence it should return independent object sets.
/// </summary>
[DataDiscoverer("Xunit.Sdk.MemberDataDiscoverer", "xunit.core")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MemberTupleDataAttribute : MemberDataAttributeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberTupleDataAttribute" /> class.
    /// </summary>
    /// <param name="memberName">The name of the public static member on the test class that will provide the test data</param>
    /// <param name="parameters">The parameters for the member (only supported for methods; ignored for everything else)</param>
    public MemberTupleDataAttribute(string memberName, params object[] parameters) : base(memberName, parameters)
    {
    }

    /// <inheritdoc />
    protected override object[] ConvertDataItem(MethodInfo testMethod, object item)
    {
        if (item is not ITuple tuple)
        {
            throw new ArgumentException($"Property {MemberName} on {MemberType} yielded an item that is not a tuple");
        }

        return ToArray(tuple);
    }

    private static object[] ToArray(ITuple tuple)
    {
        var result = new object[tuple.Length];
        for (var i = 0; i < tuple.Length; i++)
        {
            result[i] = tuple[i]!;
        }

        return result;
    }
}
