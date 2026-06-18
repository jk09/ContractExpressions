#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.NonNullStackWithUpdate;

[ContractClass(typeof(NonNullStackWithUpdatesContracts))]
internal interface INonNullStackWithUpdates
{
    bool IsEmpty { get; }
    int Count { get; }

    void Push(string value);
    string Pop();
    void UpdateAt(int index, string value);
}
