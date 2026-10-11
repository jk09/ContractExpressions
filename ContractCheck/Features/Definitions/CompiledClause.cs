using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace ContractCheck;

/// <summary>The <c>System.Diagnostics.Contracts.Contract</c> method a clause calls.</summary>
internal enum ClauseKind
{
    Requires,
    RequiresOfException,
    Assert,
    Assume,
    Ensures,
    EnsuresOnThrow,
    Invariant,
}

/// <summary>One contract clause, compiled into a predicate over an <see cref="InvocationContext"/>.</summary>
internal sealed class CompiledClause
{
    public required ClauseKind Kind { get; init; }

    public required Func<InvocationContext, bool> Predicate { get; init; }

    /// <summary>Evaluates the user message; only called when the clause fails.</summary>
    public Func<InvocationContext, string?>? Message { get; init; }

    /// <summary>The C# text of the condition, for example <c>value &gt; 0</c>.</summary>
    public required string ConditionText { get; init; }

    /// <summary>The C# text of the whole clause, for example <c>Contract.Requires(value &gt; 0)</c>.</summary>
    public required string ContractText { get; init; }

    /// <summary><c>TException</c> of <c>Requires&lt;TException&gt;</c> or <c>EnsuresOnThrow&lt;TException&gt;</c>.</summary>
    public Type? ExceptionType { get; init; }

    /// <summary>Old values to capture before the call, in slot order.</summary>
    public ImmutableArray<Func<InvocationContext, object?>> OldValueCaptures { get; init; } = [];

    /// <summary>For async members: evaluate when the returned task completes instead of when the member returns.</summary>
    public bool Deferred { get; init; }

    /// <summary>The clause references a <c>[ContractRuntimeIgnored]</c> member or type and is never evaluated.</summary>
    public bool RuntimeIgnored { get; init; }

    public ContractFailureKind FailureKind => Kind switch
    {
        ClauseKind.Requires or ClauseKind.RequiresOfException => ContractFailureKind.Precondition,
        ClauseKind.Assert => ContractFailureKind.Assert,
        ClauseKind.Assume => ContractFailureKind.Assume,
        ClauseKind.Ensures => ContractFailureKind.Postcondition,
        ClauseKind.EnsuresOnThrow => ContractFailureKind.PostconditionOnException,
        _ => ContractFailureKind.Invariant,
    };

    public bool IsEntryCheck => Kind is ClauseKind.Requires or ClauseKind.RequiresOfException or ClauseKind.Assert or ClauseKind.Assume;

    public bool HasOldValues => !OldValueCaptures.IsDefaultOrEmpty;
}
