#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Max;

/// <summary>
/// Maximum of an array (CodeContracts/Demo/Max). The demo's point: <see cref="ParseToInts"/> returns <see langword="null"/>
/// when nothing parses, and <see cref="Max"/> of an empty array returns <see cref="int.MinValue"/>, which breaks the
/// <c>Exists</c> postcondition – both are caught by the contracts.
/// </summary>
[ContractClass(typeof(MaxUtilsContracts))]
public interface IMaxUtils
{
    int Max(int[] elements);

    int[]? ParseToInts(string[] original);
}
