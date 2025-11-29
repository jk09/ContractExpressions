using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;


internal class ContractRegistry
{
    public static readonly ContractRegistry Instance = new();

    public IDictionary<MethodInfo, IList<Invokable>> Preconditions { get; } = new Dictionary<MethodInfo, IList<Invokable>>();

    public IDictionary<MethodInfo, IList<Invokable>> Postconditions { get; } = new Dictionary<MethodInfo, IList<Invokable>>();

    public IDictionary<MethodInfo, IDictionary<PropertyInfo, Delegate>> OldValueCollectors { get; } = new Dictionary<MethodInfo, IDictionary<PropertyInfo, Delegate>>();
}
