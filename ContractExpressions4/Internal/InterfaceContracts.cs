using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace ContractExpressions4.Internal;

internal sealed class InterfaceContracts
{
    private readonly ConcurrentDictionary<MethodInfo, MethodContracts> methods = new();
    private ImmutableArray<CompiledContract> invariants = [];

    public ImmutableArray<CompiledContract> Invariants => invariants;

    public void AddInvariant(CompiledContract invariant)
    {
        ImmutableInterlocked.Update(ref invariants, current => current.Add(invariant));
    }

    public void AddMethodContracts(MethodInfo method, IEnumerable<CompiledContract> contracts)
    {
        MethodContracts methodContracts = methods.GetOrAdd(method, _ => new MethodContracts());
        methodContracts.AddContracts(contracts);
    }

    public MethodContracts GetMethodContracts(MethodInfo method)
    {
        if (methods.TryGetValue(method, out MethodContracts? methodContracts))
        {
            return methodContracts;
        }

        return MethodContracts.Empty;
    }
}

internal sealed class MethodContracts
{
    public static readonly MethodContracts Empty = new();

    private ImmutableArray<CompiledContract> preconditions = [];
    private ImmutableArray<CompiledContract> postconditions = [];

    public ImmutableArray<CompiledContract> Preconditions => preconditions;

    public ImmutableArray<CompiledContract> Postconditions => postconditions;

    public void AddContracts(IEnumerable<CompiledContract> contracts)
    {
        foreach (CompiledContract contract in contracts)
        {
            if (contract.Kind == ContractKind.Precondition)
            {
                ImmutableInterlocked.Update(ref preconditions, current => current.Add(contract));
            }
            else if (contract.Kind == ContractKind.Postcondition)
            {
                ImmutableInterlocked.Update(ref postconditions, current => current.Add(contract));
            }
        }
    }
}
