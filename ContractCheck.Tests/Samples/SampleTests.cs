using System.Diagnostics.Contracts;
using ContractCheck.Samples.Addition;
using ContractCheck.Samples.BankAccount;
using ContractCheck.Samples.CreditDebit;
using ContractCheck.Samples.Devscovery;
using ContractCheck.Samples.Inventory;
using ContractCheck.Samples.Max;
using ContractCheck.Samples.Operations;
using ContractCheck.Samples.Stack;

namespace ContractCheck.Tests.Samples;

/// <summary>
/// Deterministic tests of the ContractCheck.Samples ports of CodeContracts/Demo. They combine the ContractExpressions4
/// demo tests (ContractExpressions4.Tests/Demo) with the scenarios of the legacy demo programs
/// (TestAccount.cs, UseMyMath, RuntimeChecker, StackClient, OperationClient). Property tests live in
/// ContractCheck.Samples.PropertyTests.
/// </summary>
public class BankAccountSampleTests
{
    [Fact]
    public void Overdraft_limit_getter_contract_holds()
    {
        Assert.Equal(20, Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20)).OverdraftLimit);
    }

    [Fact]
    public void Withdraw_within_limit_satisfies_postcondition()
    {
        IAccount proxy = Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));

        proxy.Withdraw(40);

        Assert.Equal(60, proxy.Amount);
    }

    [Fact]
    public void Withdraw_over_limit_violates_precondition()
    {
        ContractViolationException exception = Expect.Violation(
            ContractFailureKind.Precondition,
            () => Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20)).Withdraw(121));

        Assert.Equal("Withdraw", exception.Method);
    }

    [Fact]
    public void Overdraft_without_support_violates_invariant_at_creation()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<IAccount>(new Account(0, supportsOverdraft: false, overdraftLimit: 10)));

        Assert.Equal(ContractViolationException.CreationMethodName, exception.Method);
    }

    [Fact]
    public void Opening_negative_within_the_limit_is_valid_ported_from_TestAccount_OpenNegativeTest()
    {
        Assert.Equal(-99, Dbc.Make<IAccount>(new Account(-99, supportsOverdraft: true, overdraftLimit: 100)).Amount);
    }

    [Fact]
    public void Withdrawing_down_to_the_limit_breaks_the_strict_invariant_ported_from_TestAccount_WithdrawTest()
    {
        // Amount > -OverdraftLimit is strict: 0 - 1 == -1 is not greater than -1.
        IAccount proxy = Dbc.Make<IAccount>(new Account(0, supportsOverdraft: true, overdraftLimit: 1));

        Expect.Violation(ContractFailureKind.Invariant, () => proxy.Withdraw(1));
    }

    [Fact]
    public void Limit_above_1000_violates_invariant_ported_from_TestAccount_OpenNegativeBig()
    {
        Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<IAccount>(new Account(-2000, supportsOverdraft: true, overdraftLimit: 5000)));
    }
}

public class DevscoverySampleTests
{
    [Fact]
    public void Abs_of_a_negative_number_ported_from_UseMyMath()
    {
        Assert.Equal(122, Dbc.Make<IMyMath>(new MyMath()).AbsNetStyle(-122));
    }

    [Fact]
    public void Net_style_abs_rejects_MinValue_with_OverflowException_ported_from_UseMyMath()
    {
        // The ContractExpressions4 demo used a plain Requires (ContractViolationException); the sample follows the legacy
        // demo (CodeContracts/Demo/Devscovery/MyMath.cs), whose Requires<OverflowException> throws OverflowException (R3).
        OverflowException exception = Assert.Throws<OverflowException>(() => Dbc.Make<IMyMath>(new MyMath()).AbsNetStyle(int.MinValue));

        Assert.Equal(ContractFailureKind.Precondition, exception.GetContractFailureKind());
        Assert.Equal("x > -2147483648", exception.Data["ContractCheck.Condition"]);
    }

    [Fact]
    public void Java_style_abs_returns_MinValue()
    {
        Assert.Equal(int.MinValue, Dbc.Make<IMyMath>(new MyMath()).AbsJavaStyle(int.MinValue));
    }

    [Fact]
    public void Array_abs_rejects_null()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IArrayExtensions>(new ArrayExtensions()).Abs(null!));

        Assert.Equal("Abs", exception.Method);
    }

    [Fact]
    public void Array_abs_rejects_MinValue_elements()
    {
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IArrayExtensions>(new ArrayExtensions()).Abs([1, int.MinValue]));
    }

    [Fact]
    public void Array_abs_returns_absolute_values()
    {
        Assert.Equal(new[] { 1, 2, 0 }, Dbc.Make<IArrayExtensions>(new ArrayExtensions()).Abs([-1, 2, 0]));
    }
}

public class MaxSampleTests
{
    [Fact]
    public void Max_returns_the_maximum() => Assert.Equal(9, Dbc.Make<IMaxUtils>(new MaxUtils()).Max([3, -2, 9, 1]));

    [Fact]
    public void Max_rejects_null_ported_from_RuntimeChecker()
    {
        // RuntimeChecker passed the null returned by ParseToInts straight into Max.
        IMaxUtils proxy = Dbc.Make<IMaxUtils>(new MaxUtils());
        int[]? parsed = proxy.ParseToInts(["not", "numbers"]);

        Assert.Null(parsed);
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => proxy.Max(parsed!));
        Assert.Equal("Max", exception.Method);
    }

    [Fact]
    public void Max_of_an_empty_array_violates_postcondition() =>
        Assert.Equal("Max", Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IMaxUtils>(new MaxUtils()).Max([])).Method);

    [Fact]
    public void ParseToInts_rejects_null() =>
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IMaxUtils>(new MaxUtils()).ParseToInts(null!));
}

public class OperationsSampleTests
{
    [Fact]
    public void Adder_performs_ported_from_OperationClient() =>
        Assert.Equal(7, Dbc.Make<IBinaryOperation>(new IntAdder()).Perform(3, 4));

    [Theory]
    [InlineData(10, 4, 6)]
    public void Subtraction(int a, int b, int expected) => Assert.Equal(expected, Dbc.Make<IBinaryOperation>(new IntSub()).Perform(a, b));

    [Fact]
    public void Multiplication() => Assert.Equal(12, Dbc.Make<IBinaryOperation>(new IntMul()).Perform(3, 4));

    [Fact]
    public void Unary_minus_takes_one_argument()
    {
        IUnaryOperation proxy = Dbc.Make<IUnaryOperation>(new IntUnaryMinus());

        Assert.Equal(-5, proxy.Perform(5));
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Perform(1, 2));
    }

    [Fact]
    public void Argument_types_contract_is_inherited_by_binary_operations()
    {
        IBinaryOperation proxy = Dbc.Make<IBinaryOperation>(new IntAdder());

        Assert.Equal(2, proxy.ArgumentTypes.Length);
        Assert.All(proxy.ArgumentTypes, Assert.NotNull);
        Assert.Equal("get_ArgumentTypes", Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IBinaryOperation>(new BrokenBinaryOperation()).ArgumentTypes).Method);
    }

    [Fact]
    public void Null_arguments_violate_precondition() =>
        Assert.Equal("Perform", Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IBinaryOperation>(new IntAdder()).Perform(null!)).Method);

    [Fact]
    public void Wrong_arity_violates_precondition() =>
        Assert.Equal("Perform", Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IBinaryOperation>(new IntAdder()).Perform(2)).Method);

    [Fact]
    public void Wrong_argument_type_violates_precondition() =>
        Assert.Equal("Perform", Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IBinaryOperation>(new IntAdder()).Perform(2, "x")).Method);

    [Fact]
    public void Null_result_type_violates_postcondition()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IOperation>(new BrokenBinaryOperation()).ResultType);

        Assert.Equal("get_ResultType", exception.Method);
    }

    [Fact]
    public void Division_by_zero_is_not_part_of_the_contract()
    {
        Assert.Throws<DivideByZeroException>(() => Dbc.Make<IBinaryOperation>(new IntDiv()).Perform(1, 0));
    }

    private sealed class BrokenBinaryOperation : IBinaryOperation
    {
        public Type[] ArgumentTypes => [typeof(int)];

        public Type ResultType => null!;

        public object Perform(params object[] arguments) => 0;
    }
}

public class StackSampleTests
{
    [Fact]
    public void Pop_after_push_returns_the_item()
    {
        INonNullStack<string> proxy = Dbc.Make<INonNullStack<string>>(new NonNullStack<string>(0));
        proxy.Push("value");

        Assert.Equal("value", proxy.Pop());
        Assert.True(proxy.IsEmpty);
    }

    [Fact]
    public void Pop_of_an_empty_stack_violates_precondition()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<INonNullStack<string>>(new NonNullStack<string>(1)).Pop());

        Assert.Equal("Pop", exception.Method);
    }

    [Fact]
    public void Push_of_null_violates_precondition_ported_from_StackClient() =>
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<INonNullStack<string>>(new NonNullStack<string>(0)).Push(null!));

    [Fact]
    public void UpdateAt_with_a_valid_index_updates_the_item()
    {
        INonNullStackWithUpdates<string> proxy = Dbc.Make<INonNullStackWithUpdates<string>>(new NonNullStackWithUpdates<string>(1));
        proxy.Push("a");

        proxy.UpdateAt(0, "b");

        Assert.Equal("b", proxy.Pop());
    }

    [Fact]
    public void UpdateAt_out_of_range_violates_precondition()
    {
        INonNullStackWithUpdates<string> proxy = Dbc.Make<INonNullStackWithUpdates<string>>(new NonNullStackWithUpdates<string>(1));
        proxy.Push("a");

        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => proxy.UpdateAt(1, "b"));
        Assert.Equal("UpdateAt", exception.Method);
    }

    [Fact]
    public void Stack_with_updates_inherits_the_stack_contracts() =>
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<INonNullStackWithUpdates<string>>(new NonNullStackWithUpdates<string>(1)).Pop());
}

public class AdditionAndCreditDebitSampleTests
{
    [Theory]
    [InlineData(3, 4, 7)]
    [InlineData(-5, 10, 5)]
    [InlineData(0, 0, 0)]
    public void Adder_returns_the_sum(int a, int b, int sum) => Assert.Equal(sum, Dbc.Make<IAdder>(new Adder()).Add(a, b));

    [Fact]
    public void Balance_getter_returns_opening_balance() => Assert.Equal(100, Dbc.Make<ISimpleAccount>(new SimpleAccount(100)).Balance);

    [Fact]
    public void Credit_increases_balance()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

        proxy.Credit(25);

        Assert.Equal(125, proxy.Balance);
    }

    [Fact]
    public void Debit_decreases_balance()
    {
        ISimpleAccount proxy = Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

        proxy.Debit(40);

        Assert.Equal(60, proxy.Balance);
    }

    [Fact]
    public void Credit_of_zero_violates_precondition() =>
        Assert.Equal("Credit", Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<ISimpleAccount>(new SimpleAccount(100)).Credit(0)).Method);

    [Fact]
    public void Debit_beyond_balance_violates_precondition() =>
        Assert.Equal("Debit", Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<ISimpleAccount>(new SimpleAccount(100)).Debit(101)).Method);

    [Fact]
    public void Negative_opening_balance_violates_invariant_at_creation() =>
        Assert.Equal("<creation>", Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<ISimpleAccount>(new SimpleAccount(-1))).Method);

    [Fact]
    public void Credit_that_does_not_change_the_balance_violates_postcondition() =>
        Assert.Equal("Credit", Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<ISimpleAccount>(new BrokenCreditAccount(100)).Credit(10)).Method);

    private sealed class BrokenCreditAccount(float openingBalance) : ISimpleAccount
    {
        public float Balance { get; private set; } = openingBalance;

        public void Credit(float amount)
        {
        }

        public void Debit(float amount) => Balance -= amount;
    }
}

public class InventorySampleTests
{
    [Fact]
    public async Task Restock_then_reserve_keeps_the_async_contracts()
    {
        IInventory proxy = Dbc.Make<IInventory>(new Inventory());

        await proxy.RestockAsync("apple", 5);

        Assert.Equal(3, await proxy.ReserveAsync("apple", 2));
        Assert.Equal(3, proxy.Available("apple"));
    }

    [Fact]
    public async Task Reserving_more_than_available_throws_and_leaves_the_stock_unchanged()
    {
        IInventory proxy = Dbc.Make<IInventory>(new Inventory());
        await proxy.RestockAsync("pear", 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.ReserveAsync("pear", 2));
        Assert.Equal(1, proxy.Available("pear"));
    }

    [Fact]
    public void Invalid_arguments_violate_preconditions_synchronously()
    {
        IInventory proxy = Dbc.Make<IInventory>(new Inventory());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.ReserveAsync("", 1));
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.RestockAsync("apple", 0));
    }
}
