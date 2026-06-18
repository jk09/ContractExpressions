using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.Operations;

public class OperationsDemoTests
{
    [Fact]
    public void ArgumentTypes_WhenValid_SatisfiesGetterContracts()
    {
        IOperationDemo proxy = Dbc.Make<IOperationDemo>(new IntAdderOperation());

        Type[] argumentTypes = proxy.ArgumentTypes;

        Assert.Equal(2, argumentTypes.Length);
        Assert.All(argumentTypes, type => Assert.NotNull(type));
    }

    [Fact]
    public void ArgumentTypes_WhenBroken_ThrowsPostconditionViolation()
    {
        IOperationDemo proxy = Dbc.Make<IOperationDemo>(new BrokenArgumentTypesOperation());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() =>
        {
            _ = proxy.ArgumentTypes;
        });

        Assert.Equal(ContractKind.Postcondition, ex.Kind);
        Assert.Equal("get_ArgumentTypes", ex.Method);
    }

    [Fact]
    public void ResultType_WhenBroken_ThrowsPostconditionViolation()
    {
        IOperationDemo proxy = Dbc.Make<IOperationDemo>(new BrokenResultTypeOperation());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() =>
        {
            _ = proxy.ResultType;
        });

        Assert.Equal(ContractKind.Postcondition, ex.Kind);
        Assert.Equal("get_ResultType", ex.Method);
    }

    [Fact]
    public void Perform_WithValidArguments_ReturnsResult()
    {
        IOperationDemo proxy = Dbc.Make<IOperationDemo>(new IntAdderOperation());

        object result = proxy.Perform(2, 3);

        Assert.Equal(5, result);
    }

    [Fact]
    public void Perform_WhenArgumentsAreNull_ThrowsPreconditionViolation()
    {
        IOperationDemo proxy = Dbc.Make<IOperationDemo>(new IntAdderOperation());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Perform((object[]?)null!));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Perform", ex.Method);
    }

    [Fact]
    public void Perform_WhenArityIsWrong_ThrowsPreconditionViolation()
    {
        IOperationDemo proxy = Dbc.Make<IOperationDemo>(new IntAdderOperation());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Perform(2));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Perform", ex.Method);
    }

    [Fact]
    public void Perform_WhenArgumentTypeIsWrong_ThrowsPreconditionViolation()
    {
        IOperationDemo proxy = Dbc.Make<IOperationDemo>(new IntAdderOperation());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Perform(2, "x"));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Perform", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Perform_RandomInputs_SatisfyContracts(int left, int right) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IOperationDemo>(new IntAdderOperation()),
            (IOperationDemo proxy) => proxy.Perform(left, right));

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property ArgumentTypes_RandomInvocations_SatisfyContracts() =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IOperationDemo>(new IntAdderOperation()),
            (IOperationDemo proxy) => proxy.ArgumentTypes);

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property ResultType_RandomInvocations_SatisfyContracts() =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IOperationDemo>(new IntAdderOperation()),
            (IOperationDemo proxy) => proxy.ResultType);
}

internal sealed class BrokenArgumentTypesOperation : IOperationDemo
{
    public Type[] ArgumentTypes => [typeof(int)];

    public Type ResultType => typeof(int);

    public object Perform(params object[] arguments) => 0;
}

internal sealed class BrokenResultTypeOperation : IOperationDemo
{
    public Type[] ArgumentTypes => [typeof(int), typeof(int)];

    public Type ResultType => null!;

    public object Perform(params object[] arguments) => 0;
}
