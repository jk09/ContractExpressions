using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class Contracts
{
    public IDictionary<MethodInfo, IList<Invokable>> Preconditions { get; } = new Dictionary<MethodInfo, IList<Invokable>>();
    public IDictionary<MethodInfo, IList<Invokable>> Postconditions { get; } = new Dictionary<MethodInfo, IList<Invokable>>();
    public IDictionary<PropertyInfo, Delegate> OldValueCollectors { get; } = new Dictionary<PropertyInfo, Delegate>();
}
