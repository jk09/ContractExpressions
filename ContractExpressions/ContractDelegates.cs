using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class ContractDelegates
{
    public IDictionary<MethodInfo, Delegate> Preconditions { get; } = new Dictionary<MethodInfo, Delegate>();
    public IDictionary<MethodInfo, Delegate> Postconditions { get; } = new Dictionary<MethodInfo, Delegate>();
    public IDictionary<PropertyInfo, Delegate> OldValueCollectors { get; } = new Dictionary<PropertyInfo, Delegate>();
}
