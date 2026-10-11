using System.Collections.Concurrent;

namespace ContractCheck;

public static partial class Dbc
{
    private static readonly ConcurrentDictionary<Type, Func<object, object>> Makers = new();

    /// <summary>
    /// Non-generic <see cref="Make{T}(T)"/> for an interface known only at run time, for example a dependency-injection
    /// registration: wraps <paramref name="target"/> in a proxy of <paramref name="interfaceType"/> that checks its contracts.
    /// </summary>
    /// <param name="interfaceType">The contract interface; a closed interface type that <paramref name="target"/> implements.</param>
    /// <param name="target">The implementation to check.</param>
    /// <returns>A proxy implementing <paramref name="interfaceType"/> that forwards to <paramref name="target"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceType"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="interfaceType"/> is not a closed interface type, or <paramref name="target"/> doesn't implement it.</exception>
    /// <exception cref="InvalidOperationException">A contract class or contract definition of the interface is invalid.</exception>
    /// <exception cref="ContractViolationException">An interface invariant does not hold for <paramref name="target"/>.</exception>
    public static object Make(Type interfaceType, object target)
    {
        ArgumentNullException.ThrowIfNull(interfaceType);
        ArgumentNullException.ThrowIfNull(target);

        if (!interfaceType.IsInterface || interfaceType.ContainsGenericParameters)
        {
            throw new ArgumentException($"Dbc.Make needs a closed interface type, but {TypeNames.Format(interfaceType)} is not one.", nameof(interfaceType));
        }

        if (!interfaceType.IsInstanceOfType(target))
        {
            throw new ArgumentException($"{TypeNames.Format(target.GetType())} does not implement {TypeNames.Format(interfaceType)}.", nameof(target));
        }

        return Makers.GetOrAdd(interfaceType, static type =>
            typeof(Maker<>).MakeGenericType(type).GetMethod(nameof(Maker<object>.Make))!.CreateDelegate<Func<object, object>>())(target);
    }

    /// <summary>
    /// Whether proxies of <paramref name="interfaceType"/> have contracts to check: a contract class, or clauses defined with
    /// <c>Dbc.Def</c> for the interface or an interface it inherits contracts from. Loads the contract classes, like <see cref="Make{T}(T)"/>.
    /// </summary>
    /// <param name="interfaceType">The type to look at; any type that isn't a closed interface has no contracts.</param>
    /// <returns><see langword="true"/> if at least one clause applies to proxies of the interface.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A contract class or contract definition of the interface is invalid.</exception>
    public static bool HasContracts(Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(interfaceType);
        if (!interfaceType.IsInterface || interfaceType.ContainsGenericParameters)
        {
            return false;
        }

        ContractClassLoader.EnsureLoaded(interfaceType);
        IReadOnlyList<Type> sources = ContractAttributes.InheritanceDisabled(interfaceType) ? [interfaceType] : InterfaceHierarchy.BaseFirst(interfaceType);
        return sources.Any(static type => ContractRegistry.Find(type) is { IsEmpty: false });
    }

    private static class Maker<T>
        where T : class
    {
        public static object Make(object target) => Dbc.Make((T)target);
    }
}
