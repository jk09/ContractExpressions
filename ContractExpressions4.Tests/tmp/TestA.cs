#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Tmp;

[ContractClass(typeof(TestAContracts))]
public interface ITestA
{
    int M(int x, int y);
}

public class TestA : ITestA
{
    public int M(int x, int y) => x / y;
}
