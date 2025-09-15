using System.Reflection;

internal class ContractDelegates
{
    public readonly Dictionary<MethodInfo, IList<Delegate>> Preconditions = new();
    public readonly Dictionary<MethodInfo, IList<Delegate>> Postconditions = new();
    public readonly Dictionary<PropertyInfo, Delegate> OldValueCollectors = new();

    public static readonly ContractDelegates Empty = new();
}
