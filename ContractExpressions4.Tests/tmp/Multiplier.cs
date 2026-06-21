#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Tmp;

[ContractClass(typeof(MultiplierContracts))]
public interface IMultiplier
{
    int Multiply(int a, int b);
}

public class Multiplier : IMultiplier
{
    public int Multiply(int a, int b) => checked(a * b);
}
