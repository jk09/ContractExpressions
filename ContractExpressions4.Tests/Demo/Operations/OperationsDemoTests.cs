using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.Operations;

public class OperationsDemoTests
{
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

    [Property]
    public Property Perform_RandomInputs_SatisfyContracts(int left, int right) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IOperationDemo>(new IntAdderOperation()),
            (IOperationDemo proxy) => proxy.Perform(left, right));
}
