#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests.CE1;

/// <summary>
/// Tests inspired by CodeContracts/Demo/BankAccount/Account.cs.
/// Covers: Requires, Ensures, OldValue on float properties, Invariant, Result.
/// </summary>
public class BankAccountContractTests : IClassFixture<ContractFailureUnwindFixture>
{
    private readonly IAccount _proxy;

    public BankAccountContractTests()
    {
        _proxy = Dbc.Make<IAccount>(new Account(100f, 50f));
    }

    [Fact]
    public void Deposit_HappyPath_IncreasesAmount()
    {
        _proxy.Deposit(25f);
        Assert.Equal(125f, _proxy.Amount);
    }

    [Fact]
    public void Deposit_ZeroAmount_ThrowsPreconditionFailure()
    {
        var ex = Assert.ThrowsAny<Exception>(() => _proxy.Deposit(0f));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Deposit_NegativeAmount_ThrowsPreconditionFailure()
    {
        var ex = Assert.ThrowsAny<Exception>(() => _proxy.Deposit(-10f));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Withdraw_HappyPath_DecreasesAmount()
    {
        _proxy.Withdraw(30f);
        Assert.Equal(70f, _proxy.Amount);
    }

    [Fact]
    public void Withdraw_ZeroAmount_ThrowsPreconditionFailure()
    {
        var ex = Assert.ThrowsAny<Exception>(() => _proxy.Withdraw(0f));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Withdraw_ExceedsBalancePlusOverdraft_ThrowsPreconditionFailure()
    {
        // Amount=100, OverdraftLimit=50 → max withdraw is 150
        var ex = Assert.ThrowsAny<Exception>(() => _proxy.Withdraw(151f));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Withdraw_ExactlyBalancePlusOverdraft_Succeeds()
    {
        _proxy.Withdraw(150f);
        Assert.Equal(-50f, _proxy.Amount);
    }

    [Fact]
    public void Withdraw_UsesOverdraft_Succeeds()
    {
        _proxy.Withdraw(120f);
        Assert.Equal(-20f, _proxy.Amount);
    }

    [Fact]
    public void GetOverdraftLimit_ReturnsCorrectValue()
    {
        Assert.Equal(50f, _proxy.OverdraftLimit);
    }

    [Fact]
    public void Deposit_ThenWithdraw_OldValueTracksCorrectly()
    {
        _proxy.Deposit(50f);   // 100 → 150
        _proxy.Withdraw(75f);  // 150 → 75
        Assert.Equal(75f, _proxy.Amount);
    }
}

[ContractClass(typeof(AccountContracts))]
interface IAccount
{
    float Amount { get; }
    float OverdraftLimit { get; }
    void Deposit(float amount);
    void Withdraw(float amount);
}

class Account(float openingAmount, float overdraftLimit) : IAccount
{
    public float Amount { get; private set; } = openingAmount;
    public float OverdraftLimit { get; } = overdraftLimit;

    public void Deposit(float amount)
    {
        Amount += amount;
    }

    public void Withdraw(float amount)
    {
        Amount -= amount;
    }
}

[ContractClassFor(typeof(IAccount))]
class AccountContracts
{
    public AccountContracts()
    {
        // Deposit: amount must be positive; ensures Amount increases by amount
        Dbc.Def(static (IAccount x, float amount) => x.Deposit(amount),
                static (IAccount x, float amount) => Contract.Requires(amount > 0),
                static (IAccount x, float amount) => Contract.Ensures(x.Amount == Contract.OldValue<float>(x.Amount) + amount));

        // Withdraw: amount must be positive and within balance + overdraft; ensures Amount decreases by amount
        Dbc.Def(static (IAccount x, float amount) => x.Withdraw(amount),
                static (IAccount x, float amount) => Contract.Requires(amount > 0),
                static (IAccount x, float amount) => Contract.Requires(amount <= x.Amount + x.OverdraftLimit),
                static (IAccount x, float amount) => Contract.Ensures(x.Amount == Contract.OldValue<float>(x.Amount) - amount));

        // OverdraftLimit getter: result is non-negative
        Dbc.Def(static (IAccount x) => x.OverdraftLimit,
                static (IAccount x) => Contract.Ensures(Contract.Result<float>() >= 0));
    }
}
