#define CONTRACTS_FULL

using System.Collections;
using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Compatibility.Suites;

/// <summary>
/// Ported from ContractExpressions.Tests/CE-1/BankAccountContractTests.cs and its ContractExpressions4 copy.
/// Change: violations assert <c>ContractViolationException</c> + <c>Kind</c> (resolution R1).
/// </summary>
public class BankAccountContractTests
{
    private readonly IAccount proxy = Dbc.Make<IAccount>(new Account(100f, 50f));

    [Fact]
    public void Deposit_increases_amount()
    {
        proxy.Deposit(25f);

        Assert.Equal(125f, proxy.Amount);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-10f)]
    public void Deposit_of_non_positive_amount_violates_precondition(float amount) =>
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Deposit(amount));

    [Fact]
    public void Withdraw_decreases_amount()
    {
        proxy.Withdraw(30f);

        Assert.Equal(70f, proxy.Amount);
    }

    [Fact]
    public void Withdraw_of_zero_violates_precondition() => Expect.Violation(ContractFailureKind.Precondition, () => proxy.Withdraw(0f));

    [Fact]
    public void Withdraw_beyond_balance_plus_overdraft_violates_precondition() =>
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Withdraw(151f));

    [Fact]
    public void Withdraw_of_exactly_balance_plus_overdraft_succeeds()
    {
        proxy.Withdraw(150f);

        Assert.Equal(-50f, proxy.Amount);
    }

    [Fact]
    public void Withdraw_into_the_overdraft_succeeds()
    {
        proxy.Withdraw(120f);

        Assert.Equal(-20f, proxy.Amount);
    }

    [Fact]
    public void Overdraft_limit_getter_returns_the_limit() => Assert.Equal(50f, proxy.OverdraftLimit);

    [Fact]
    public void Deposit_then_withdraw_tracks_old_values()
    {
        proxy.Deposit(50f);
        proxy.Withdraw(75f);

        Assert.Equal(75f, proxy.Amount);
    }
}

[ContractClass(typeof(AccountContracts))]
public interface IAccount
{
    float Amount { get; }

    float OverdraftLimit { get; }

    void Deposit(float amount);

    void Withdraw(float amount);
}

public sealed class Account(float openingAmount, float overdraftLimit) : IAccount
{
    public float Amount { get; private set; } = openingAmount;

    public float OverdraftLimit { get; } = overdraftLimit;

    public void Deposit(float amount) => Amount += amount;

    public void Withdraw(float amount) => Amount -= amount;
}

[ContractClassFor(typeof(IAccount))]
internal sealed class AccountContracts
{
    public AccountContracts()
    {
        Dbc.Def(static (IAccount x, float amount) => x.Deposit(amount),
            static (IAccount x, float amount) => Contract.Requires(amount > 0),
            static (IAccount x, float amount) => Contract.Ensures(x.Amount == Contract.OldValue(x.Amount) + amount));

        Dbc.Def(static (IAccount x, float amount) => x.Withdraw(amount),
            static (IAccount x, float amount) => Contract.Requires(amount > 0),
            static (IAccount x, float amount) => Contract.Requires(amount <= x.Amount + x.OverdraftLimit),
            static (IAccount x, float amount) => Contract.Ensures(x.Amount == Contract.OldValue(x.Amount) - amount));

        Dbc.Def(static (IAccount x) => x.OverdraftLimit,
            static (IAccount x) => Contract.Ensures(Contract.Result<float>() >= 0));
    }
}

/// <summary>Ported from ContractExpressions(4).Tests/CE-1/StackContractTests.cs.</summary>
public class StackContractTests
{
    private readonly IStack proxy = Dbc.Make<IStack>(new SimpleStack());

    [Fact]
    public void Push_increases_count()
    {
        proxy.Push("item1");

        Assert.Equal(1, proxy.Count);
    }

    [Fact]
    public void Push_of_null_violates_precondition() => Expect.Violation(ContractFailureKind.Precondition, () => proxy.Push(null!));

    [Fact]
    public void Pop_returns_the_last_pushed_item()
    {
        proxy.Push("a");
        proxy.Push("b");

        Assert.Equal("b", proxy.Pop());
    }

    [Fact]
    public void Pop_of_an_empty_stack_violates_precondition() => Expect.Violation(ContractFailureKind.Precondition, () => proxy.Pop());

    [Fact]
    public void Pop_result_is_not_null()
    {
        proxy.Push("item");

        Assert.NotNull(proxy.Pop());
    }

    [Fact]
    public void Push_increases_count_by_one()
    {
        proxy.Push("first");
        Assert.Equal(1, proxy.Count);

        proxy.Push("second");
        Assert.Equal(2, proxy.Count);
    }

    [Fact]
    public void Pop_decreases_count_by_one()
    {
        proxy.Push("a");
        proxy.Push("b");
        Assert.Equal(2, proxy.Count);

        proxy.Pop();

        Assert.Equal(1, proxy.Count);
    }

    [Fact]
    public void Push_then_pop_round_trips()
    {
        proxy.Push("x");
        proxy.Push("y");
        proxy.Push("z");

        Assert.Equal("z", proxy.Pop());
        Assert.Equal("y", proxy.Pop());
        Assert.Equal("x", proxy.Pop());
        Assert.Equal(0, proxy.Count);
    }

    [Fact]
    public void IsEmpty_when_no_items() => Assert.True(proxy.IsEmpty);

    [Fact]
    public void IsEmpty_after_push_is_false()
    {
        proxy.Push("item");

        Assert.False(proxy.IsEmpty);
    }
}

[ContractClass(typeof(StackContracts))]
public interface IStack
{
    int Count { get; }

    bool IsEmpty { get; }

    void Push(object item);

    object Pop();
}

public sealed class SimpleStack : IStack
{
    private readonly List<object> items = [];

    public int Count => items.Count;

    public bool IsEmpty => items.Count == 0;

    public void Push(object item) => items.Add(item);

    public object Pop()
    {
        object item = items[^1];
        items.RemoveAt(items.Count - 1);
        return item;
    }
}

[ContractClassFor(typeof(IStack))]
internal sealed class StackContracts
{
    public StackContracts()
    {
        Dbc.Def(static (IStack x, object item) => x.Push(item),
            static (IStack x, object item) => Contract.Requires(item != null),
            static (IStack x, object item) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) + 1));

        Dbc.Def(static (IStack x) => x.Pop(),
            static (IStack x) => Contract.Requires(!x.IsEmpty),
            static (IStack x) => Contract.Ensures(Contract.Result<object>() != null),
            static (IStack x) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) - 1));
    }
}

/// <summary>Ported from ContractExpressions(4).Tests/CE-1/OperationsContractTests.cs.</summary>
public class OperationsContractTests
{
    private readonly ICalculator proxy = Dbc.Make<ICalculator>(new Calculator());

    [Theory]
    [InlineData(3, 4, 7)]
    [InlineData(-1, -2, -3)]
    [InlineData(5, 0, 5)]
    public void Add_returns_the_sum(int a, int b, int sum) => Assert.Equal(sum, proxy.Add(a, b));

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(10, -2, -5)]
    public void Divide_returns_the_quotient(int a, int divisor, int quotient) => Assert.Equal(quotient, proxy.Divide(a, divisor));

    [Fact]
    public void Divide_by_zero_violates_precondition() => Expect.Violation(ContractFailureKind.Precondition, () => proxy.Divide(10, 0));
}

[ContractClass(typeof(CalculatorContracts))]
public interface ICalculator
{
    int Add(int a, int b);

    int Divide(int a, int divisor);
}

public sealed class Calculator : ICalculator
{
    public int Add(int a, int b) => a + b;

    public int Divide(int a, int divisor) => a / divisor;
}

[ContractClassFor(typeof(ICalculator))]
internal sealed class CalculatorContracts
{
    public CalculatorContracts()
    {
        Dbc.Def(static (ICalculator x, int a, int b) => x.Add(a, b),
            static (ICalculator x, int a, int b) => Contract.Ensures(Contract.Result<int>() == a + b));

        Dbc.Def(static (ICalculator x, int a, int divisor) => x.Divide(a, divisor),
            static (ICalculator x, int a, int divisor) => Contract.Requires(divisor != 0));
    }
}

/// <summary>
/// Ported from ContractExpressions(4).Tests/MyDictionaryContractTests.cs, which had no tests: its only definition has no
/// clauses (the out-parameter clauses were commented out because selectors cannot express out arguments).
/// Kept to show that such a definition is accepted (resolution R16) and that out parameters flow through the proxy.
/// </summary>
public class MyDictionaryContractTests
{
    [Fact]
    public void Definition_without_clauses_is_accepted_and_out_parameters_work()
    {
        IMyDictionary proxy = Dbc.Make<IMyDictionary>(new MyDictionary());
        proxy.Add("key", "value");

        Assert.True(proxy.ContainsKey("key"));
        Assert.True(proxy.TryGetValue("key", out object value));
        Assert.Equal("value", value);
        Assert.False(proxy.TryGetValue("missing", out _));
    }
}

[ContractClass(typeof(MyDictionaryContracts))]
public interface IMyDictionary : IDictionary
{
    bool TryGetValue(string key, out object value);

    bool ContainsKey(string key);
}

public sealed class MyDictionary : Hashtable, IMyDictionary
{
    public bool ContainsKey(string key) => Contains(key);

    public bool TryGetValue(string key, out object value)
    {
        value = this[key]!;
        return ContainsKey(key);
    }
}

[ContractClassFor(typeof(IMyDictionary))]
internal sealed class MyDictionaryContracts
{
    public MyDictionaryContracts() => Dbc.Def(static (IMyDictionary dict, string key) => dict.ContainsKey(key));
}
