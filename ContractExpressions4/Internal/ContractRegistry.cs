using System.Collections.Concurrent;
using System.Reflection;

namespace ContractExpressions4.Internal;

internal sealed class ContractRegistry
{
    private readonly ConcurrentDictionary<Type, InterfaceContracts> interfaceContracts = new();

    public static ContractRegistry Instance { get; } = new();

    public InterfaceContracts GetOrCreate(Type interfaceType) => interfaceContracts.GetOrAdd(interfaceType, _ => new InterfaceContracts());

    public InterfaceContracts Get(Type interfaceType)
    {
        if (interfaceContracts.TryGetValue(interfaceType, out InterfaceContracts? contracts))
        {
            return contracts;
        }

        return new InterfaceContracts();
    }

    public void AddMethodContracts(Type interfaceType, MethodInfo method, IEnumerable<CompiledContract> contracts)
    {
        GetOrCreate(interfaceType).AddMethodContracts(method, contracts);
    }

    public void AddInvariant(Type interfaceType, CompiledContract contract)
    {
        GetOrCreate(interfaceType).AddInvariant(contract);
    }
}
