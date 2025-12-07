using System.Diagnostics.Contracts;

namespace ContractExpressions.Tests;

public class ContractFailedFixture : IDisposable
{
    public ContractFailedFixture()
    {
        Contract.ContractFailed += OnContractFailed;
    }
    public void Dispose()
    {
        Contract.ContractFailed -= OnContractFailed;
    }

    private static void OnContractFailed(object? sender, ContractFailedEventArgs e)
    {
        e.SetUnwind();
    }


}