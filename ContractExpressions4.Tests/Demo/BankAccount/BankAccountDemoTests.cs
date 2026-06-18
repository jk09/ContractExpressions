using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.BankAccount;

public class BankAccountDemoTests
{
    [Fact]
    public void OverdraftLimit_GetterContract_IsEnforced()
    {
        IAccount proxy = Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));

        float limit = proxy.OverdraftLimit;

        Assert.Equal(20, limit);
    }

    [Fact]
    public void Withdraw_WithValidAmount_SatisfiesPostcondition()
    {
        IAccount proxy = Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));

        proxy.Withdraw(40);

        Assert.Equal(60, proxy.Amount);
    }

    [Fact]
    public void Withdraw_WhenOverLimit_ThrowsPreconditionViolation()
    {
        IAccount proxy = Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Withdraw(121));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Withdraw", ex.Method);
    }

    [Fact]
    public void Make_WhenInvariantFailsAtCreation_ThrowsInvariantViolation()
    {
        ContractViolationException ex = Assert.Throws<ContractViolationException>(() =>
            Dbc.Make<IAccount>(new Account(0, supportsOverdraft: false, overdraftLimit: 10)));

        Assert.Equal(ContractKind.Invariant, ex.Kind);
        Assert.Equal("<creation>", ex.Method);
    }

    [Property]
    public Property Deposit_RandomInputs_SatisfyContracts(float deposit) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20)),
            (IAccount proxy) => proxy.Deposit(deposit));

    [Property]
    public Property Withdraw_RandomInputs_SatisfyContracts(float amount) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20)),
            (IAccount proxy) => proxy.Withdraw(amount));
}
