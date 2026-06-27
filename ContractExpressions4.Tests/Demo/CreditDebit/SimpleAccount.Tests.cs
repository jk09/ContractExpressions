using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.CreditDebit;

public class SimpleAccountTests : IClassFixture<ContractFailureUnwindFixture>
{
    [Fact]
    public void Balance_Getter_ReturnsOpeningBalance()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

        Assert.Equal(100, proxy.Balance);
    }

    [Fact]
    public void Credit_WithPositiveAmount_IncreasesBalance()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

        proxy.Credit(25);

        Assert.Equal(125, proxy.Balance);
    }

    [Fact]
    public void Debit_WithValidAmount_DecreasesBalance()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

        proxy.Debit(40);

        Assert.Equal(60, proxy.Balance);
    }

    [Fact]
    public void Credit_WithNonPositiveAmount_ThrowsPreconditionViolation()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Credit(0));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Credit", ex.Method);
    }

    [Fact]
    public void Debit_WhenAmountExceedsBalance_ThrowsPreconditionViolation()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Debit(101));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Debit", ex.Method);
    }

    [Fact]
    public void Make_WhenOpeningBalanceIsNegative_ThrowsInvariantViolationAtCreation()
    {
        ContractViolationException ex = Assert.Throws<ContractViolationException>(() =>
            Dbc.Make<ISimpleAccount>(new SimpleAccount(-1)));

        Assert.Equal(ContractKind.Invariant, ex.Kind);
        Assert.Equal("<creation>", ex.Method);
    }

    [Fact]
    public void Credit_WhenImplementationViolatesPostcondition_ThrowsPostconditionViolation()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new BrokenCreditAccount(100));

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Credit(10));

        Assert.Equal(ContractKind.Postcondition, ex.Kind);
        Assert.Equal("Credit", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Credit_RandomInputs_SatisfyContracts(float amount) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<ISimpleAccount>(new SimpleAccount(100)),
            (ISimpleAccount proxy) => proxy.Credit(amount));

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Debit_RandomInputs_SatisfyContracts(float amount) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<ISimpleAccount>(new SimpleAccount(100)),
            (ISimpleAccount proxy) => proxy.Debit(amount));

    private sealed class BrokenCreditAccount(float openingBalance) : ISimpleAccount
    {
        private float balance = openingBalance;

        public float Balance => balance;

        public void Credit(float amount)
        {
            // Intentionally violates the postcondition: no state change.
        }

        public void Debit(float amount)
        {
            balance -= amount;
        }
    }
}
