#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.Addition;

[ContractClass(typeof(AdderContracts))]
internal interface IAdder
{
    int Add(int a, int b);
}
