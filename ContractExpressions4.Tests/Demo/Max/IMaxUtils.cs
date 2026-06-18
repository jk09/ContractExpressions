#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.Max;

[ContractClass(typeof(MaxUtilsContracts))]
internal interface IMaxUtils
{
    int Max(int[] elements);
    int[]? ParseToInts(string[] original);
}
