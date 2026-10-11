using System.Diagnostics.Contracts;

namespace ContractCheck.Tests;

/// <summary>Assertions for contract failures and contract definition errors.</summary>
internal static class Expect
{
    public static ContractViolationException Violation(ContractFailureKind kind, Action action)
    {
        ContractViolationException exception = Assert.Throws<ContractViolationException>(action);
        Assert.Equal(kind, exception.Kind);
        return exception;
    }

    public static ContractViolationException Violation(ContractFailureKind kind, Func<object?> function)
    {
        ContractViolationException exception = Assert.Throws<ContractViolationException>(function);
        Assert.Equal(kind, exception.Kind);
        return exception;
    }

    public static async Task<ContractViolationException> ViolationAsync(ContractFailureKind kind, Func<Task> action)
    {
        ContractViolationException exception = await Assert.ThrowsAsync<ContractViolationException>(action);
        Assert.Equal(kind, exception.Kind);
        return exception;
    }

    /// <summary>Asserts that a contract definition is rejected with a message containing every fragment.</summary>
    public static InvalidOperationException DefinitionError(Action define, params string[] fragments)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(define);
        Assert.StartsWith("Invalid contract definition", exception.Message, StringComparison.Ordinal);
        foreach (string fragment in fragments)
        {
            Assert.Contains(fragment, exception.Message, StringComparison.Ordinal);
        }

        return exception;
    }
}

/// <summary>
/// Tests that subscribe to the process-wide <see cref="Contract.ContractFailed"/> event must not run while other tests
/// raise contract failures, or a SetHandled() handler would swallow their failures.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ContractFailedHandlers
{
    public const string Name = "Contract.ContractFailed handlers";
}
