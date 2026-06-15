namespace ContractExpressions4;

public sealed class ContractViolationException : Exception
{
    public ContractViolationException(ContractKind kind, string method, string contractText)
        : base($"Contract {kind.ToString().ToLowerInvariant()} failed for '{method}': {contractText}")
    {
        Kind = kind;
        Method = method;
        ContractText = contractText;
    }

    public ContractKind Kind { get; }

    public string Method { get; }

    public string ContractText { get; }
}
