using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpr;

internal class Invokable
{
    public required string Representation { get; init; }
    public required Delegate Delegate { get; init; }
}

internal class ContractDelegates
{
    public IDictionary<MethodInfo, IList<Invokable>> Preconditions { get; } = new Dictionary<MethodInfo, IList<Invokable>>();
    public IDictionary<MethodInfo, IList<Invokable>> Postconditions { get; } = new Dictionary<MethodInfo, IList<Invokable>>();
    public IDictionary<PropertyInfo, Delegate> OldValueCollectors { get; } = new Dictionary<PropertyInfo, Delegate>();
    public static readonly ContractDelegates Empty = new();
}
