using System.Diagnostics.Contracts;

namespace ContractCheck;

/// <summary>Helpers for recognising exceptions caused by contract failures.</summary>
public static class ContractFailureExtensions
{
    internal const string FailureKindKey = "ContractCheck.FailureKind";
    internal const string ConditionKey = "ContractCheck.Condition";
    internal const string ClauseKey = "ContractCheck.Clause";

    /// <summary>
    /// Returns the kind of the contract failure that caused <paramref name="exception"/>: the
    /// <see cref="ContractViolationException.Kind"/> of a <see cref="ContractViolationException"/>, or
    /// <see cref="ContractFailureKind.Precondition"/> for an exception thrown by a <c>Contract.Requires&lt;TException&gt;</c> clause.
    /// Returns <see langword="null"/> for any other exception.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    public static ContractFailureKind? GetContractFailureKind(this Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is ContractViolationException violation)
        {
            return violation.Kind;
        }

        return exception.Data[FailureKindKey] is ContractFailureKind kind ? kind : null;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="exception"/> was thrown by a check made before the member ran:
    /// <c>Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c> or <c>Assume</c>.
    /// Such failures mean the caller passed arguments, or used the object in a state, that the contract rejects.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    public static bool IsEntryCheckFailure(this Exception exception) =>
        exception.GetContractFailureKind() is ContractFailureKind.Precondition or ContractFailureKind.Assert or ContractFailureKind.Assume;
}
