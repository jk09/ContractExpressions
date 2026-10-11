using System.Reflection;

namespace ContractCheck;

/// <summary>The <see cref="DispatchProxy"/> behind <see cref="Dbc.Make{T}(T)"/>: forwards every call to the target through <see cref="ContractInvoker"/>.</summary>
/// <typeparam name="T">The contract interface.</typeparam>
internal class ContractAwareProxy<T> : DispatchProxy
    where T : class
{
    private T target = null!;

    internal void Initialize(T wrapped) => target = wrapped;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        return ContractInvoker.Invoke(typeof(T), target, targetMethod, args ?? []);
    }
}
