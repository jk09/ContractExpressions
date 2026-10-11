namespace ContractCheck;

/// <summary>
/// Which clause kinds proxies check – the runtime checking levels of CodeContracts. Set it with
/// <see cref="Dbc.CheckingLevel"/> or the runtimeconfig setting <c>ContractCheck.CheckingLevel</c>.
/// </summary>
public enum ContractCheckingLevel
{
    /// <summary>No checks: proxies forward every call.</summary>
    None,

    /// <summary>Only <c>Contract.Requires&lt;TException&gt;</c>, the preconditions that throw a specific exception.</summary>
    ReleaseRequires,

    /// <summary><c>Contract.Requires</c> and <c>Contract.Requires&lt;TException&gt;</c>.</summary>
    Preconditions,

    /// <summary>Preconditions and postconditions (<c>Ensures</c>, <c>EnsuresOnThrow</c>); no invariants, <c>Assert</c> or <c>Assume</c>.</summary>
    PreAndPost,

    /// <summary>Everything, including interface and member invariants, <c>Assert</c> and <c>Assume</c>. The default.</summary>
    Full,
}
