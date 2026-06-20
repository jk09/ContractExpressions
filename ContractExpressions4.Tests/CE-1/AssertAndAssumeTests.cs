#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.CE1;

/// <summary>
/// First test coverage for Contract.Assert and Contract.Assume.
/// Assert/Assume are treated as preconditions (evaluated before the method call).
/// Covers: Assert(bool), Assert(bool, string), Assume(bool), Assume(bool, string).
/// </summary>
public class AssertAndAssumeTests : IClassFixture<ContractFailureUnwindFixture>
{
    // --- Assert tests ---

    [Fact]
    public void Assert_ConditionTrue_Passes()
    {
        var proxy = Dbc.Make<IAssertValidator>(new AssertValidatorImpl());
        var result = proxy.Validate(5);
        Assert.True(result);
    }

    [Fact]
    public void Assert_ConditionFalse_Throws()
    {
        var proxy = Dbc.Make<IAssertValidator>(new AssertValidatorImpl());
        var ex = Assert.Throws<ContractViolationException>(() => proxy.Validate(-1));
        Assert.Equal(ContractKind.Precondition, ex.Kind);
    }

    [Fact]
    public void Assert_WithMessage_MessageAppearsInException()
    {
        var proxy = Dbc.Make<IAssertValidatorWithMessage>(new AssertValidatorWithMessageImpl());
        var ex = Assert.Throws<ContractViolationException>(() => proxy.Validate(-1));
        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Contains("Value must be positive", ex.Message);
    }

    // --- Assume tests ---

    [Fact]
    public void Assume_ConditionTrue_Passes()
    {
        var proxy = Dbc.Make<IAssumeValidator>(new AssumeValidatorImpl());
        var result = proxy.Validate(500);
        Assert.True(result);
    }

    [Fact]
    public void Assume_ConditionFalse_Throws()
    {
        var proxy = Dbc.Make<IAssumeValidator>(new AssumeValidatorImpl());
        var ex = Assert.Throws<ContractViolationException>(() => proxy.Validate(1001));
        Assert.Equal(ContractKind.Precondition, ex.Kind);
    }

    [Fact]
    public void Assume_WithMessage_MessageAppearsInException()
    {
        var proxy = Dbc.Make<IAssumeValidatorWithMessage>(new AssumeValidatorWithMessageImpl());
        var ex = Assert.Throws<ContractViolationException>(() => proxy.Validate(1001));
        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Contains("Value assumed under limit", ex.Message);
    }
}

// --- Assert interfaces ---

[ContractClass(typeof(AssertValidatorContracts))]
interface IAssertValidator
{
    bool Validate(int value);
}

class AssertValidatorImpl : IAssertValidator
{
    public bool Validate(int value) => value > 0;
}

[ContractClassFor(typeof(IAssertValidator))]
class AssertValidatorContracts
{
    public AssertValidatorContracts()
    {
        Dbc.Def(static (IAssertValidator x, int value) => x.Validate(value),
                static (IAssertValidator x, int value) => Contract.Assert(value > 0));
    }
}

[ContractClass(typeof(AssertValidatorWithMessageContracts))]
interface IAssertValidatorWithMessage
{
    bool Validate(int value);
}

class AssertValidatorWithMessageImpl : IAssertValidatorWithMessage
{
    public bool Validate(int value) => value > 0;
}

[ContractClassFor(typeof(IAssertValidatorWithMessage))]
class AssertValidatorWithMessageContracts
{
    public AssertValidatorWithMessageContracts()
    {
        Dbc.Def(static (IAssertValidatorWithMessage x, int value) => x.Validate(value),
                static (IAssertValidatorWithMessage x, int value) => Contract.Assert(value > 0, "Value must be positive"));
    }
}

// --- Assume interfaces ---

[ContractClass(typeof(AssumeValidatorContracts))]
interface IAssumeValidator
{
    bool Validate(int value);
}

class AssumeValidatorImpl : IAssumeValidator
{
    public bool Validate(int value) => value <= 1000;
}

[ContractClassFor(typeof(IAssumeValidator))]
class AssumeValidatorContracts
{
    public AssumeValidatorContracts()
    {
        Dbc.Def(static (IAssumeValidator x, int value) => x.Validate(value),
                static (IAssumeValidator x, int value) => Contract.Assume(value <= 1000));
    }
}

[ContractClass(typeof(AssumeValidatorWithMessageContracts))]
interface IAssumeValidatorWithMessage
{
    bool Validate(int value);
}

class AssumeValidatorWithMessageImpl : IAssumeValidatorWithMessage
{
    public bool Validate(int value) => value <= 1000;
}

[ContractClassFor(typeof(IAssumeValidatorWithMessage))]
class AssumeValidatorWithMessageContracts
{
    public AssumeValidatorWithMessageContracts()
    {
        Dbc.Def(static (IAssumeValidatorWithMessage x, int value) => x.Validate(value),
                static (IAssumeValidatorWithMessage x, int value) => Contract.Assume(value <= 1000, "Value assumed under limit"));
    }
}
