using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ContractCheck;

/// <summary>A clause plus the index of its old values in <see cref="InvocationContext.OldValues"/> (-1 when it has none).</summary>
internal readonly record struct BoundClause(CompiledClause Clause, int OldValueIndex);

/// <summary>
/// Everything a proxy of one interface checks for one member at one checking level, merged from the interface and its
/// base interfaces (base first) and grouped by evaluation stage. Cached per (interface, member, level) until new clauses
/// are registered.
/// </summary>
internal sealed class MemberContracts
{
    private static readonly ConcurrentDictionary<(Type Interface, MethodInfo Member, ContractCheckingLevel Level), MemberContracts> Cache = new(KeyComparer.Instance);
    private static readonly ConcurrentDictionary<Type, (int Version, ImmutableArray<BoundClause> Invariants)> CreationCache = new();

    private readonly int version;

    private MemberContracts(Type interfaceType, MethodInfo member, int version)
    {
        InterfaceType = interfaceType;
        Member = member;
        this.version = version;
    }

    public Type InterfaceType { get; }

    public MethodInfo Member { get; }

    /// <summary><c>Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c> and <c>Assume</c>, in definition order.</summary>
    public ImmutableArray<BoundClause> EntryChecks { get; private init; } = [];

    /// <summary>Clauses whose old values are captured before the call; their index is <see cref="BoundClause.OldValueIndex"/>.</summary>
    public ImmutableArray<BoundClause> OldValueClauses { get; private init; } = [];

    public ImmutableArray<BoundClause> OnThrowChecks { get; private init; } = [];

    /// <summary>Checks on normal return: postconditions, then member invariants, then interface invariants (unless pure).</summary>
    public ImmutableArray<BoundClause> ExitChecks { get; private init; } = [];

    /// <summary>For async members: the checks made when the returned task completes successfully.</summary>
    public ImmutableArray<BoundClause> CompletionChecks { get; private init; } = [];

    public bool HasChecks { get; private init; }

    /// <summary>The proxy returns a task that completes after <see cref="CompletionChecks"/> and <see cref="OnThrowChecks"/> ran.</summary>
    public bool WrapsReturnedTask { get; private init; }

    public static MemberContracts For(Type interfaceType, MethodInfo member)
    {
        ContractCheckingLevel level = CheckingLevels.Current;
        int current = ContractRegistry.Version;
        if (Cache.TryGetValue((interfaceType, member, level), out MemberContracts? cached) && cached.version == current)
        {
            return cached;
        }

        MemberContracts built = Build(interfaceType, member, current, level);
        Cache[(interfaceType, member, level)] = built;
        return built;
    }

    /// <summary>The interface invariants <see cref="Dbc.Make{T}(T)"/> checks before returning a proxy (none below <see cref="ContractCheckingLevel.Full"/>).</summary>
    public static ImmutableArray<BoundClause> CreationInvariants(Type interfaceType)
    {
        if (!CheckingLevels.Includes(CheckingLevels.Current, ClauseKind.Invariant))
        {
            return [];
        }

        int current = ContractRegistry.Version;
        if (CreationCache.TryGetValue(interfaceType, out var cached) && cached.Version == current)
        {
            return cached.Invariants;
        }

        ImmutableArray<BoundClause> invariants = ContractAttributes.RuntimeCheckingDisabled(interfaceType, member: null)
            ? []
            : [.. Sources(interfaceType).SelectMany(static c => c.Invariants).Where(static c => !c.RuntimeIgnored).Select(static c => new BoundClause(c, -1))];
        CreationCache[interfaceType] = (current, invariants);
        return invariants;
    }

    /// <summary>Evaluates the old-value captures; a capture that throws is stored and rethrown only if a clause reads it.</summary>
    public void CaptureOldValues(InvocationContext context)
    {
        if (OldValueClauses.IsEmpty)
        {
            return;
        }

        var all = new CapturedValue[]?[OldValueClauses.Length];
        foreach (BoundClause bound in OldValueClauses)
        {
            ImmutableArray<Func<InvocationContext, object?>> captures = bound.Clause.OldValueCaptures;
            var values = new CapturedValue[captures.Length];
            for (int slot = 0; slot < captures.Length; slot++)
            {
                try
                {
                    values[slot] = new CapturedValue(captures[slot](context), null);
                }
                catch (Exception exception)
                {
                    values[slot] = new CapturedValue(null, ExceptionDispatchInfo.Capture(exception));
                }
            }

            all[bound.OldValueIndex] = values;
        }

        context.OldValues = all;
    }

    private static MemberContracts Build(Type interfaceType, MethodInfo member, int version, ContractCheckingLevel level)
    {
        if (level == ContractCheckingLevel.None || ContractAttributes.RuntimeCheckingDisabled(interfaceType, member))
        {
            return new MemberContracts(interfaceType, member, version);
        }

        // Clauses the level leaves out are dropped here, so they cost nothing: no old values, no task wrapping.
        List<CompiledClause> clauses = [];
        List<CompiledClause> invariants = [];
        foreach (InterfaceContracts source in Sources(interfaceType))
        {
            clauses.AddRange(source.ClausesFor(member).Where(c => !c.RuntimeIgnored && CheckingLevels.Includes(level, c.Kind)));
            invariants.AddRange(source.Invariants.Where(c => !c.RuntimeIgnored && CheckingLevels.Includes(level, c.Kind)));
        }

        int oldValueIndex = 0;
        List<BoundClause> bound = [.. clauses.Select(c => new BoundClause(c, c.HasOldValues ? oldValueIndex++ : -1))];
        bool awaitable = Awaitables.IsAwaitable(member.ReturnType, out _);
        IEnumerable<BoundClause> interfaceInvariants = ContractAttributes.IsPure(member)
            ? Enumerable.Empty<BoundClause>()
            : invariants.Select(static c => new BoundClause(c, -1));

        // Legacy CodeContracts order at exit: postconditions, then invariants. Async members check them on completion.
        ImmutableArray<BoundClause> Stage(bool deferred) =>
        [
            .. bound.Where(b => b.Clause.Kind == ClauseKind.Ensures && b.Clause.Deferred == deferred),
            .. bound.Where(b => b.Clause.Kind == ClauseKind.Invariant && b.Clause.Deferred == deferred),
            .. (awaitable == deferred ? interfaceInvariants : Enumerable.Empty<BoundClause>()),
        ];

        ImmutableArray<BoundClause> entry = [.. bound.Where(static b => b.Clause.IsEntryCheck)];
        ImmutableArray<BoundClause> onThrow = [.. bound.Where(static b => b.Clause.Kind == ClauseKind.EnsuresOnThrow)];
        ImmutableArray<BoundClause> exit = Stage(deferred: false);
        ImmutableArray<BoundClause> completion = Stage(deferred: true);

        return new MemberContracts(interfaceType, member, version)
        {
            EntryChecks = entry,
            OldValueClauses = [.. bound.Where(static b => b.OldValueIndex >= 0)],
            OnThrowChecks = onThrow,
            ExitChecks = exit,
            CompletionChecks = completion,
            HasChecks = !(entry.IsEmpty && onThrow.IsEmpty && exit.IsEmpty && completion.IsEmpty),
            WrapsReturnedTask = awaitable && !(completion.IsEmpty && onThrow.IsEmpty),
        };
    }

    private static IEnumerable<InterfaceContracts> Sources(Type interfaceType)
    {
        IReadOnlyList<Type> types = ContractAttributes.InheritanceDisabled(interfaceType) ? [interfaceType] : InterfaceHierarchy.BaseFirst(interfaceType);
        foreach (Type type in types)
        {
            if (ContractRegistry.Find(type) is { } contracts)
            {
                yield return contracts;
            }
        }
    }

    private sealed class KeyComparer : IEqualityComparer<(Type Interface, MethodInfo Member, ContractCheckingLevel Level)>
    {
        public static readonly KeyComparer Instance = new();

        public bool Equals((Type Interface, MethodInfo Member, ContractCheckingLevel Level) x, (Type Interface, MethodInfo Member, ContractCheckingLevel Level) y) =>
            x.Interface == y.Interface && x.Level == y.Level && MethodIdentity.Comparer.Equals(x.Member, y.Member);

        public int GetHashCode((Type Interface, MethodInfo Member, ContractCheckingLevel Level) obj) =>
            HashCode.Combine(obj.Interface, obj.Level, MethodIdentity.Comparer.GetHashCode(obj.Member));
    }
}
