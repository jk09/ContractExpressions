#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Stack;

[ContractClassFor(typeof(INonNullStack<>))]
internal sealed class NonNullStackContracts<T>
    where T : class
{
    public NonNullStackContracts()
    {
        Dbc.Def(static (INonNullStack<T> s, T value) => s.Push(value),
            static (INonNullStack<T> s, T value) => Contract.Requires(value != null),
            static (INonNullStack<T> s, T value) => Contract.Ensures(s.Count == Contract.OldValue(s.Count) + 1));

        Dbc.Def(static (INonNullStack<T> s) => s.Pop(),
            static (INonNullStack<T> s) => Contract.Requires(!s.IsEmpty),
            static (INonNullStack<T> s) => Contract.Ensures(Contract.Result<T>() != null),
            static (INonNullStack<T> s) => Contract.Ensures(s.Count == Contract.OldValue(s.Count) - 1));

        Dbc.Def(static (INonNullStack<T> s) => Contract.Invariant(s.Count >= 0),
            static (INonNullStack<T> s) => Contract.Invariant(s.IsEmpty == (s.Count == 0)));
    }
}

[ContractClassFor(typeof(INonNullStackWithUpdates<>))]
internal sealed class NonNullStackWithUpdatesContracts<T>
    where T : class
{
    public NonNullStackWithUpdatesContracts()
    {
        Dbc.Def(static (INonNullStackWithUpdates<T> s, int index, T value) => s.UpdateAt(index, value),
            static (INonNullStackWithUpdates<T> s, int index, T value) => Contract.Requires(index >= 0),
            static (INonNullStackWithUpdates<T> s, int index, T value) => Contract.Requires(index < s.Count),
            static (INonNullStackWithUpdates<T> s, int index, T value) => Contract.Requires(value != null),
            static (INonNullStackWithUpdates<T> s, int index, T value) => Contract.Ensures(s.Count == Contract.OldValue(s.Count)));
    }
}
