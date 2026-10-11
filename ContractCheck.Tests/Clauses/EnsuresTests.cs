#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.Ensures;

/// <summary>
/// <c>Contract.Ensures</c> and <c>Contract.Result&lt;T&gt;()</c>. Includes ContractExpressions(4).Tests/PostconditionOverloadTests.cs
/// and the postcondition cases of CodeContracts Foxtrot/Tests/RewrittenContractTest.cs.
/// </summary>
public class EnsuresTests
{
    [Fact]
    public void Null_result_violates_postcondition_ported_from_PostconditionOverloadTests()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IProducer>(new Producer()).GetValue());

        Assert.Equal("GetValue", exception.Method);
        Assert.Equal("Contract.Result<object>() != null", exception.Condition);
    }

    [Fact]
    public void Postcondition_message_is_reported_ported_from_PostconditionOverloadTests()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IProducer>(new Producer()).GetValueWithMessage());

        Assert.Contains("Result cannot be null", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Boolean_result_ported_from_EnsuresReturnsTrue(bool mustBeTrue)
    {
        IProducer proxy = Dbc.Make<IProducer>(new Producer());

        if (mustBeTrue)
        {
            Assert.True(proxy.ReturnsTrue(true));
        }
        else
        {
            Expect.Violation(ContractFailureKind.Postcondition, () => proxy.ReturnsTrue(false));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Postcondition_without_result_ported_from_EnsuresTrue(bool mustBeTrue)
    {
        IProducer proxy = Dbc.Make<IProducer>(new Producer());

        if (mustBeTrue)
        {
            proxy.EnsuresTrue(true);
        }
        else
        {
            Expect.Violation(ContractFailureKind.Postcondition, () => proxy.EnsuresTrue(false));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Result_can_be_dereferenced_ported_from_PostConditionUsingResultAsCallee(bool behave)
    {
        IProducer proxy = Dbc.Make<IProducer>(new Producer());

        if (behave)
        {
            Assert.Equal("abc", proxy.ThreeLetters(true));
        }
        else
        {
            Expect.Violation(ContractFailureKind.Postcondition, () => proxy.ThreeLetters(false));
        }
    }

    [Fact]
    public void Result_relates_to_the_arguments_ported_from_OperationsContractTests()
    {
        IProducer proxy = Dbc.Make<IProducer>(new Producer());

        Assert.Equal(7, proxy.Add(3, 4));
        Assert.Equal(-3, proxy.Add(-1, -2));
        Assert.Equal(5, proxy.Add(5, 0));
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.Add(int.MaxValue, 1));
    }

    [Fact]
    public void Property_getter_postcondition_is_checked()
    {
        Assert.Equal(3, Dbc.Make<IProducer>(new Producer { Size = 3 }).Size);
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IProducer>(new Producer { Size = -3 }).Size);
    }

    [Fact]
    public void Postconditions_are_not_checked_when_the_member_throws()
    {
        // Probe S24: both source projects agreed; kept.
        Assert.Throws<InvalidOperationException>(() => Dbc.Make<IProducer>(new Producer()).Throws());
    }

    [Fact]
    public void All_postconditions_are_checked_in_definition_order()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IProducer>(new Producer()).Negative());

        Assert.Equal("Contract.Result<int>() >= 0", exception.Condition);
    }
}

[ContractClass(typeof(ProducerContracts))]
public interface IProducer
{
    int Size { get; }

    object GetValue();

    object GetValueWithMessage();

    bool ReturnsTrue(bool mustBeTrue);

    void EnsuresTrue(bool mustBeTrue);

    string ThreeLetters(bool behave);

    int Add(int a, int b);

    int Throws();

    int Negative();
}

public sealed class Producer : IProducer
{
    public int Size { get; set; }

    public object GetValue() => null!;

    public object GetValueWithMessage() => null!;

    public bool ReturnsTrue(bool mustBeTrue) => mustBeTrue;

    public void EnsuresTrue(bool mustBeTrue)
    {
    }

    public string ThreeLetters(bool behave) => behave ? "abc" : "wrongResult";

    public int Add(int a, int b) => unchecked(a + b);

    public int Throws() => throw new InvalidOperationException("no result");

    public int Negative() => -1;
}

[ContractClassFor(typeof(IProducer))]
internal sealed class ProducerContracts
{
    public ProducerContracts()
    {
        Dbc.Def(static (IProducer x) => x.Size,
            static (IProducer x) => Contract.Ensures(Contract.Result<int>() >= 0));

        Dbc.Def(static (IProducer x) => x.GetValue(),
            static (IProducer x) => Contract.Ensures(Contract.Result<object>() != null));

        Dbc.Def(static (IProducer x) => x.GetValueWithMessage(),
            static (IProducer x) => Contract.Ensures(Contract.Result<object>() != null, "Result cannot be null"));

        Dbc.Def(static (IProducer x, bool mustBeTrue) => x.ReturnsTrue(mustBeTrue),
            static (IProducer x, bool mustBeTrue) => Contract.Ensures(Contract.Result<bool>()));

        Dbc.Def(static (IProducer x, bool mustBeTrue) => x.EnsuresTrue(mustBeTrue),
            static (IProducer x, bool mustBeTrue) => Contract.Ensures(mustBeTrue));

        Dbc.Def(static (IProducer x, bool behave) => x.ThreeLetters(behave),
            static (IProducer x, bool behave) => Contract.Ensures(Contract.Result<string>().Length == 3));

        Dbc.Def(static (IProducer x, int a, int b) => x.Add(a, b),
            static (IProducer x, int a, int b) => Contract.Ensures(Contract.Result<int>() == (long)a + b));

        Dbc.Def(static (IProducer x) => x.Throws(),
            static (IProducer x) => Contract.Ensures(false));

        Dbc.Def(static (IProducer x) => x.Negative(),
            static (IProducer x) => Contract.Ensures(Contract.Result<int>() >= 0),
            static (IProducer x) => Contract.Ensures(Contract.Result<int>() > 10));
    }
}
