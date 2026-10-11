#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractCheck.FsCheck;
using FsCheck;
using FsCheck.Xunit;

namespace ContractCheck.Tests.Compatibility.Ce4;

/// <summary>
/// Ported from ContractExpressions4.Tests/DbcApiFixtureTests.cs. Change: <c>ContractKind</c> became the BCL
/// <c>ContractFailureKind</c> (resolution R2).
/// </summary>
public class DbcApiFixtureTests
{
    [Fact]
    public void Add_with_valid_input_satisfies_contracts()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        Assert.Equal(2, proxy.Add(2));
        Assert.Equal(2, proxy.Count);
    }

    [Fact]
    public void Add_when_precondition_fails_throws_precondition_violation()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<ICounter>(new Counter()).Add(0));

        Assert.Equal("Add", exception.Method);
    }

    [Fact]
    public void Add_when_postcondition_fails_throws_postcondition_violation()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IBrokenCounter>(new BrokenCounter()).Add(2));

        Assert.Equal("Add", exception.Method);
    }

    [Property]
    public Property Add_random_inputs_satisfy_contracts(int value) =>
        DbcPropertyTest.Check(() => Dbc.Make<ICounter>(new Counter()), proxy => proxy.Add(value));

    [Fact]
    public void Make_when_invariant_fails_at_creation_throws_invariant_violation()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<IInvariantCounter>(new InvalidInvariantCounter()));

        Assert.Equal("<creation>", exception.Method);
    }

    [Fact]
    public void Spend_when_invariant_fails_after_method_throws_invariant_violation()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<IInvariantCounter>(new ValidInvariantCounter()).Spend(2));

        Assert.Equal("Spend", exception.Method);
    }
}

[ContractClass(typeof(CounterContracts))]
public interface ICounter
{
    int Count { get; }

    int Add(int value);
}

public sealed class Counter : ICounter
{
    public int Count { get; private set; }

    public int Add(int value) => Count = unchecked(Count + value);
}

[ContractClassFor(typeof(ICounter))]
internal sealed class CounterContracts
{
    public CounterContracts()
    {
        Dbc.Def(static (ICounter x, int value) => x.Add(value),
            static (ICounter x, int value) => Contract.Requires(value > 0),
            static (ICounter x, int value) => Contract.Ensures(Contract.Result<int>() == Contract.OldValue(x.Count) + value),
            static (ICounter x, int value) => Contract.Ensures(x.Count == Contract.Result<int>()));

        Dbc.Def(static (ICounter x) => Contract.Invariant(x.Count >= 0));
    }
}

[ContractClass(typeof(BrokenCounterContracts))]
public interface IBrokenCounter
{
    int Count { get; }

    int Add(int value);
}

public sealed class BrokenCounter : IBrokenCounter
{
    public int Count { get; private set; }

    public int Add(int value)
    {
        Count += value;
        return Count + 1;
    }
}

[ContractClassFor(typeof(IBrokenCounter))]
internal sealed class BrokenCounterContracts
{
    public BrokenCounterContracts() =>
        Dbc.Def(static (IBrokenCounter x, int value) => x.Add(value),
            static (IBrokenCounter x, int value) => Contract.Ensures(Contract.Result<int>() == x.Count));
}

[ContractClass(typeof(InvariantCounterContracts))]
public interface IInvariantCounter
{
    int Balance { get; }

    void Spend(int amount);
}

public sealed class InvalidInvariantCounter : IInvariantCounter
{
    public int Balance => -1;

    public void Spend(int amount)
    {
    }
}

public sealed class ValidInvariantCounter : IInvariantCounter
{
    public int Balance { get; private set; } = 1;

    public void Spend(int amount) => Balance -= amount;
}

[ContractClassFor(typeof(IInvariantCounter))]
internal sealed class InvariantCounterContracts
{
    public InvariantCounterContracts() => Dbc.Def(static (IInvariantCounter x) => Contract.Invariant(x.Balance >= 0));
}

/// <summary>Ported from ContractExpressions4.Tests/tmp/Multiplier.Tests.cs (output of the earlier code-generation agent).</summary>
public class MultiplierTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(12, 7)]
    public void Multiply_with_valid_input_satisfies_contracts(int a, int b)
    {
        int result = Dbc.Make<IMultiplier>(new Multiplier()).Multiply(a, b);

        Assert.Equal(a * b, result);
        Assert.True(result > 0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 2)]
    [InlineData(2, -1)]
    public void Multiply_when_precondition_fails_throws_precondition_violation(int a, int b)
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IMultiplier>(new Multiplier()).Multiply(a, b));

        Assert.Equal("Multiply", exception.Method);
    }

    [Property]
    public Property Multiply_positive_small_inputs_satisfy_contracts(byte a, byte b) =>
        DbcPropertyTest.Check(() => Dbc.Make<IMultiplier>(new Multiplier()), proxy => proxy.Multiply(a + 1, b + 1));
}

[ContractClass(typeof(MultiplierContracts))]
public interface IMultiplier
{
    int Multiply(int a, int b);
}

public sealed class Multiplier : IMultiplier
{
    public int Multiply(int a, int b) => checked(a * b);
}

[ContractClassFor(typeof(IMultiplier))]
internal sealed class MultiplierContracts
{
    public MultiplierContracts() =>
        Dbc.Def(static (IMultiplier x, int a, int b) => x.Multiply(a, b),
            static (IMultiplier x, int a, int b) => Contract.Requires(a > 0 && b > 0),
            static (IMultiplier x, int a, int b) => Contract.Ensures(Contract.Result<int>() > 0));
}

/// <summary>Ported from ContractExpressions4.Tests/tmp/TestA.Tests.cs.</summary>
public class DividerTests
{
    [Theory]
    [InlineData(10, 2)]
    [InlineData(-9, 3)]
    [InlineData(7, -2)]
    public void Divide_with_valid_input_satisfies_contracts(int x, int y) =>
        Assert.Equal(x / y, Dbc.Make<IDivider>(new Divider()).M(x, y));

    [Theory]
    [InlineData(10, 0)]
    [InlineData(-5, 0)]
    public void Divide_by_zero_throws_precondition_violation(int x, int y)
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IDivider>(new Divider()).M(x, y));

        Assert.Equal("M", exception.Method);
    }

    [Property]
    public Property Divide_random_inputs_satisfy_contracts(int x, int y) =>
        DbcPropertyTest.Check(() => Dbc.Make<IDivider>(new Divider()), proxy => proxy.M(x, y));

    [Fact]
    public void Overflowing_division_is_rejected_by_the_completed_contract()
    {
        // The original contract only required y != 0, so M(int.MinValue, -1) threw OverflowException – exactly the kind
        // of gap a property test finds. The ported contract adds the missing precondition.
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IDivider>(new Divider()).M(int.MinValue, -1));
    }
}

[ContractClass(typeof(DividerContracts))]
public interface IDivider
{
    int M(int x, int y);
}

public sealed class Divider : IDivider
{
    public int M(int x, int y) => x / y;
}

[ContractClassFor(typeof(IDivider))]
internal sealed class DividerContracts
{
    public DividerContracts() =>
        Dbc.Def(static (IDivider t, int x, int y) => t.M(x, y),
            static (IDivider t, int x, int y) => Contract.Requires(y != 0),
            static (IDivider t, int x, int y) => Contract.Requires(x != int.MinValue || y != -1));
}
