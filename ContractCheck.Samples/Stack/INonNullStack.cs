#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Stack;

/// <summary>A stack that never holds <see langword="null"/> (CodeContracts/Demo/Stack/NonNullStack.cs).</summary>
/// <remarks>The contract class is generic, like the interface: <c>Dbc.Make</c> closes it over the interface's type argument.</remarks>
[ContractClass(typeof(NonNullStackContracts<>))]
public interface INonNullStack<T>
    where T : class
{
    bool IsEmpty { get; }

    int Count { get; }

    void Push(T value);

    T Pop();
}

/// <summary>A non-null stack whose items can be replaced in place (CodeContracts/Demo/NonNullStackWithUpdate); inherits the stack contracts.</summary>
[ContractClass(typeof(NonNullStackWithUpdatesContracts<>))]
public interface INonNullStackWithUpdates<T> : INonNullStack<T>
    where T : class
{
    void UpdateAt(int index, T value);
}
