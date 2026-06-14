using System.Collections.Immutable;

namespace ContractExpressions4.Internal;

internal sealed record CompiledContract(
    ContractKind Kind,
    string SourceText,
    string Token,
    Func<ContractInvocationContext, bool> Predicate,
    ImmutableArray<OldValueCapture> OldValueCaptures);

internal sealed record OldValueCapture(int Slot, Func<ContractInvocationContext, object?> Reader);
