using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace ContractCheck;

/// <summary>
/// Process-wide store of compiled clauses, one <see cref="InterfaceContracts"/> per interface that clauses were defined for.
/// Clauses are scoped to the interface named by the clause lambdas: contracts written for <c>IMyList</c> apply to
/// <c>IMyList</c> proxies (and proxies of interfaces deriving from it), never to other interfaces that share a base member.
/// </summary>
internal static class ContractRegistry
{
    private static readonly ConcurrentDictionary<Type, InterfaceContracts> Interfaces = new();
    private static int version;

    /// <summary>Changes whenever a clause is registered; caches of resolved contracts compare against it.</summary>
    public static int Version => Volatile.Read(ref version);

    public static InterfaceContracts For(Type interfaceType) => Interfaces.GetOrAdd(interfaceType, static t => new InterfaceContracts(t));

    public static InterfaceContracts? Find(Type interfaceType) => Interfaces.TryGetValue(interfaceType, out InterfaceContracts? contracts) ? contracts : null;

    internal static void Changed() => Interlocked.Increment(ref version);
}

/// <summary>The clauses defined for one interface: interface invariants and clauses per member.</summary>
internal sealed class InterfaceContracts(Type interfaceType)
{
    private readonly object gate = new();
    private ImmutableArray<CompiledClause> invariants = [];
    private ImmutableDictionary<MethodInfo, ImmutableArray<CompiledClause>> members =
        ImmutableDictionary.Create<MethodInfo, ImmutableArray<CompiledClause>>(MethodIdentity.Comparer);

    public Type InterfaceType { get; } = interfaceType;

    public ImmutableArray<CompiledClause> Invariants => invariants;

    /// <summary>No invariant and no member clause has been defined.</summary>
    public bool IsEmpty => invariants.IsEmpty && members.IsEmpty;

    public ImmutableArray<CompiledClause> ClausesFor(MethodInfo method) =>
        members.TryGetValue(method, out ImmutableArray<CompiledClause> clauses) ? clauses : [];

    public void AddInvariants(IEnumerable<CompiledClause> clauses)
    {
        lock (gate)
        {
            invariants = invariants.AddRange(clauses);
        }

        ContractRegistry.Changed();
    }

    public void AddMemberClauses(MethodInfo method, IEnumerable<CompiledClause> clauses)
    {
        lock (gate)
        {
            members = members.SetItem(method, ClausesFor(method).AddRange(clauses));
        }

        ContractRegistry.Changed();
    }
}

/// <summary>
/// Compares methods by identity, including constructed generic methods, which reflection may hand out as different
/// <see cref="MethodInfo"/> instances (one from the expression tree, one from the proxy).
/// </summary>
internal sealed class MethodIdentity : IEqualityComparer<MethodInfo>
{
    public static readonly MethodIdentity Comparer = new();

    public bool Equals(MethodInfo? x, MethodInfo? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null || x.MetadataToken != y.MetadataToken || x.Module != y.Module || x.DeclaringType != y.DeclaringType)
        {
            return false;
        }

        return !x.IsGenericMethod || x.GetGenericArguments().SequenceEqual(y.GetGenericArguments());
    }

    public int GetHashCode(MethodInfo obj) => HashCode.Combine(obj.MetadataToken, obj.DeclaringType);
}
