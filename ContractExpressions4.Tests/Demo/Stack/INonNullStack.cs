#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.Stack;

[ContractClass(typeof(NonNullStackContracts))]
internal interface INonNullStack
{
    bool IsEmpty { get; }
    int Count { get; }

    void Push(string value);
    string Pop();
}
