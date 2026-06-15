using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace ContractExpressions4.Internal;

internal static class ContractClassLoader
{
    private static readonly ConcurrentDictionary<Type, bool> LoadedInterfaces = new();

    public static void EnsureLoaded(Type interfaceType)
    {
        LoadedInterfaces.GetOrAdd(interfaceType, static t =>
        {
            ContractClassAttribute? contractClass = t.GetCustomAttributes(typeof(ContractClassAttribute), inherit: false)
                .Cast<ContractClassAttribute>()
                .SingleOrDefault();

            if (contractClass?.TypeContainingContracts is null)
            {
                return true;
            }

            _ = Activator.CreateInstance(contractClass.TypeContainingContracts, nonPublic: true)
                ?? throw new InvalidOperationException($"Failed to create contract class '{contractClass.TypeContainingContracts.FullName}'.");

            return true;
        });
    }
}
