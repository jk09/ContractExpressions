using System.Diagnostics.Contracts;

namespace ContractCheck;

/// <summary>
/// Formats failure messages exactly like <c>ContractHelper.RaiseContractFailedEvent</c> does
/// (<c>"Precondition failed: value &gt; 0  user message"</c>), for the cases where that method returns no message.
/// </summary>
internal static class FailureMessages
{
    public static string Describe(ContractFailureKind kind, string? condition, string? userMessage)
    {
        string text = string.IsNullOrEmpty(condition) ? Title(kind) + "." : Title(kind) + ": " + condition;
        return string.IsNullOrEmpty(userMessage) ? text : text + "  " + userMessage;
    }

    public static string Title(ContractFailureKind kind) => kind switch
    {
        ContractFailureKind.Precondition => "Precondition failed",
        ContractFailureKind.Postcondition => "Postcondition failed",
        ContractFailureKind.PostconditionOnException => "Postcondition failed after throwing an exception",
        ContractFailureKind.Invariant => "Invariant failed",
        ContractFailureKind.Assert => "Assertion failed",
        ContractFailureKind.Assume => "Assumption failed",
        _ => kind + " failed",
    };
}
