using System.Diagnostics.Contracts;

namespace ContractExpressions;

internal sealed class ContractViolationException : Exception
{
    public ContractFailureKind ContractFailureKind { get; init; }

    public ContractViolationException(ContractFailureKind kind, string? message = null) : base(message)
    {
        ContractFailureKind = kind;
    }
}
