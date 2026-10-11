using System.Diagnostics.Contracts;
using System.Reflection;

namespace ContractCheck;

/// <summary>
/// Thrown by a <see cref="Dbc.Make{T}(T)"/> proxy when a contract clause does not hold
/// and no <see cref="Contract.ContractFailed"/> handler marked the failure as handled.
/// </summary>
/// <remarks>
/// <c>Contract.Requires&lt;TException&gt;</c> clauses throw <c>TException</c> instead; use
/// <see cref="ContractFailureExtensions.GetContractFailureKind(Exception)"/> to recognise both.
/// </remarks>
public sealed class ContractViolationException : Exception
{
    /// <summary>The member name reported for invariant checks made by <see cref="Dbc.Make{T}(T)"/>.</summary>
    public const string CreationMethodName = "<creation>";

    /// <summary>Creates an exception for a failed contract clause.</summary>
    /// <param name="kind">The kind of the failed clause.</param>
    /// <param name="method">The name of the checked member, or <see cref="CreationMethodName"/>.</param>
    /// <param name="contractText">The source text of the failed clause.</param>
    public ContractViolationException(ContractFailureKind kind, string method, string contractText)
        : this(kind, method, contractText, condition: null, userMessage: null, interfaceType: null, member: null,
               message: $"{FailureMessages.Describe(kind, contractText, null)} (in {method})", innerException: null)
    {
    }

    internal ContractViolationException(
        ContractFailureKind kind,
        string method,
        string contractText,
        string? condition,
        string? userMessage,
        Type? interfaceType,
        MethodInfo? member,
        string message,
        Exception? innerException)
        : base(message, innerException)
    {
        Kind = kind;
        Method = method;
        ContractText = contractText;
        Condition = condition;
        UserMessage = userMessage;
        InterfaceType = interfaceType;
        Member = member;
    }

    /// <summary>The kind of the failed clause.</summary>
    public ContractFailureKind Kind { get; }

    /// <summary>The name of the checked member (for example <c>Add</c> or <c>get_Count</c>), or <see cref="CreationMethodName"/>.</summary>
    public string Method { get; }

    /// <summary>The source text of the failed clause, for example <c>Contract.Requires(value &gt; 0)</c>.</summary>
    public string ContractText { get; }

    /// <summary>The condition of the failed clause, for example <c>value &gt; 0</c>.</summary>
    public string? Condition { get; }

    /// <summary>The user message of the failed clause, if it has one.</summary>
    public string? UserMessage { get; }

    /// <summary>The contract interface whose proxy detected the failure.</summary>
    public Type? InterfaceType { get; }

    /// <summary>The checked interface member, or <see langword="null"/> for checks made by <see cref="Dbc.Make{T}(T)"/>.</summary>
    public MethodInfo? Member { get; }
}
