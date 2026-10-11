using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ContractCheck;

/// <summary>Everything needed to report one failed clause.</summary>
internal readonly record struct ContractFailure(
    ContractFailureKind Kind,
    string Condition,
    string? UserMessage,
    string ContractText,
    Type InterfaceType,
    MethodInfo? Member,
    Exception? ThrownException = null,
    Type? TypedException = null);

/// <summary>
/// Reports contract failures the way the CodeContracts runtime did: raise <see cref="Contract.ContractFailed"/> first,
/// then throw unless a handler called <see cref="ContractFailedEventArgs.SetHandled"/>.
/// </summary>
internal static class FailureReporter
{
    // The BCL throws this internal type from RaiseContractFailedEvent when a handler calls SetUnwind() or throws.
    private const string BclContractExceptionName = "System.Diagnostics.Contracts.ContractException";

    /// <summary>Returns normally only when the failure was handled by a <see cref="Contract.ContractFailed"/> handler.</summary>
    public static void Report(in ContractFailure failure)
    {
        string? displayMessage;
        Exception? unwound = null;
        try
        {
            displayMessage = ContractHelper.RaiseContractFailedEvent(failure.Kind, failure.UserMessage, failure.Condition, failure.ThrownException);
        }
        catch (Exception exception) when (exception.GetType().FullName == BclContractExceptionName)
        {
            displayMessage = exception.Message;
            unwound = exception;
        }

        if (failure.TypedException is not null)
        {
            // Requires<TException> always throws TException: the member cannot run with arguments it rejects.
            throw TypedExceptionFactory.Create(
                failure.TypedException,
                displayMessage ?? FailureMessages.Describe(failure.Kind, failure.Condition, failure.UserMessage),
                failure.UserMessage,
                failure.Condition);
        }

        if (displayMessage is null)
        {
            return;
        }

        throw new ContractViolationException(
            failure.Kind,
            failure.Member?.Name ?? ContractViolationException.CreationMethodName,
            failure.ContractText,
            failure.Condition,
            failure.UserMessage,
            failure.InterfaceType,
            failure.Member,
            $"{displayMessage} ({Location(failure.InterfaceType, failure.Member)})",
            failure.ThrownException ?? unwound);
    }

    private static string Location(Type interfaceType, MethodInfo? member) =>
        member is null
            ? $"in {TypeNames.Format(interfaceType)}, checked by Dbc.Make"
            : $"in {TypeNames.Format(interfaceType)}.{member.Name}";
}
