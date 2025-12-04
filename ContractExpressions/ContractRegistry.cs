using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;


internal class ContractRegistry
{
    public static readonly ContractRegistry Instance = new();

    public IDictionary<MethodInfo, IList<Invokable>> Preconditions { get; } = new ConcurrentDictionary<MethodInfo, IList<Invokable>>();

    public IDictionary<MethodInfo, IList<Invokable>> Postconditions { get; } = new ConcurrentDictionary<MethodInfo, IList<Invokable>>();

    public IDictionary<MethodInfo, IDictionary<PropertyInfo, Delegate>> OldValueCollectors { get; } = new ConcurrentDictionary<MethodInfo, IDictionary<PropertyInfo, Delegate>>();
}