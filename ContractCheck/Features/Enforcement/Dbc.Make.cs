using System.Reflection;

namespace ContractCheck;

public static partial class Dbc
{
    /// <summary>
    /// Wraps <paramref name="target"/> in a proxy of <typeparamref name="T"/> that checks the contracts of
    /// <typeparamref name="T"/> and of the interfaces it extends on every call.
    /// </summary>
    /// <remarks>
    /// Runs the <see cref="System.Diagnostics.Contracts.ContractClassAttribute">contract classes</see> of the interfaces once,
    /// then checks the interface invariants of <paramref name="target"/> before returning. Wrapping a ContractCheck proxy
    /// of <typeparamref name="T"/> again returns it unchanged.
    /// </remarks>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="target">The implementation to check.</param>
    /// <returns>A proxy that forwards to <paramref name="target"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an interface.</exception>
    /// <exception cref="InvalidOperationException">A contract class or contract definition of <typeparamref name="T"/> is invalid.</exception>
    /// <exception cref="ContractViolationException">An interface invariant does not hold for <paramref name="target"/>.</exception>
    public static T Make<T>(T target)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(target);

        Type interfaceType = typeof(T);
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException($"Dbc.Make needs an interface type, but {TypeNames.Format(interfaceType)} is not an interface.");
        }

        if (target is ContractAwareProxy<T>)
        {
            return target;
        }

        ContractClassLoader.EnsureLoaded(interfaceType);
        ContractInvoker.CheckCreation(interfaceType, target);

        T proxy = DispatchProxy.Create<T, ContractAwareProxy<T>>();
        ((ContractAwareProxy<T>)(object)proxy).Initialize(target);
        return proxy;
    }
}
