#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Devscovery;

/// <summary>Element-wise absolute value of an array (CodeContracts/Demo/Devscovery/ArrayExtensions.cs).</summary>
[ContractClass(typeof(ArrayExtensionsContracts))]
public interface IArrayExtensions
{
    int[] Abs(int[] xs);
}
