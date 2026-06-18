#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.Devscovery;

[ContractClass(typeof(ArrayExtensionsDemoContracts))]
internal interface IArrayExtensionsDemo
{
    int[] Abs(int[] xs);
}
