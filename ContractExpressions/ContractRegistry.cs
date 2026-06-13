using System.Collections.Concurrent;
using System.Reflection;

namespace ContractExpressions;


internal class ContractRegistry
{
    public static readonly ContractRegistry Instance = new();

    public ConcurrentDictionary<MethodInfo, IList<Invokable>> Preconditions { get; } = new();

    public ConcurrentDictionary<MethodInfo, IList<Invokable>> Postconditions { get; } = new();

    public ConcurrentDictionary<MethodInfo, IList<Invokable>> PostconditionsOnThrow { get; } = new();

    public ConcurrentDictionary<MethodInfo, IList<Invokable>> Invariants { get; } = new();

    public ConcurrentDictionary<MethodInfo, IDictionary<PropertyInfo, Delegate>> OldValueCollectors { get; } = new();
}