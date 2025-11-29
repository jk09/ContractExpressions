using System.Reflection;

namespace ContractExpressions;

internal static class ContractRegistry
{
    private static Dictionary<Type, Contracts> Contracts { get; } = new();

    public static void AddPreconditions(Type intfType, MethodInfo method, IList<Invokable> preconditions)
    {
        if (!Contracts.TryGetValue(intfType, out var contracts))
        {
            contracts = new Contracts();
            Contracts[intfType] = contracts;
        }

        contracts.Preconditions[method] = preconditions;
    }
    public static void AddPostconditions(Type intfType, MethodInfo method, IList<Invokable> postconditions)
    {
        if (!Contracts.TryGetValue(intfType, out var contracts))
        {
            contracts = new Contracts();
            Contracts[intfType] = contracts;
        }

        contracts.Postconditions[method] = postconditions;
    }
    public static void AddOldValueCollector(Type intfType, PropertyInfo property, Delegate collector)
    {
        if (!Contracts.TryGetValue(intfType, out var contracts))
        {
            contracts = new Contracts();
            Contracts[intfType] = contracts;
        }

        contracts.OldValueCollectors[property] = collector;
    }

    public static Contracts Get(Type intfType)
    {
        return Contracts[intfType];
    }
}
