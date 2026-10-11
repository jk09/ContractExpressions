# 0002. Report failures through Contract.ContractFailed and a public ContractViolationException

- Status: Accepted
- Date: 2026-10-11
- Features: Failures
- Spec: amber-harbor-v9p6ri

## Context
ContractExpressions threw the BCL's internal `ContractException` and subscribed a new unwind handler on every check (a
leak that also made `SetHandled` useless); ContractExpressions4 threw a public `ContractViolationException` and never
raised `Contract.ContractFailed`. Legacy CodeContracts raised `Contract.ContractFailed` (handlers may `SetHandled` or
`SetUnwind`) and then threw the internal `ContractException`, which callers can't catch by type.

## Options considered
1. **Event only, BCL behaviour** – exact legacy semantics; the thrown exception is internal and untyped for callers.
2. **Public exception only** – easy to catch; no handler hook for logging or for tests that continue past a failure.
3. **Event first, then a public exception** – legacy hook kept; failures are catchable by type with details.

## Decision
Option 3: every failure raises `Contract.ContractFailed`; `SetHandled()` lets the call continue; otherwise ContractCheck
throws `ContractViolationException` (`Kind`, `Method`, `Condition`, `UserMessage`, `ContractText`, `InterfaceType`,
`Member`) with the BCL message format. `Requires<TException>` keeps the legacy rule and always throws `TException`.

## Consequences
- Tests and callers catch one public type; tools ask `GetContractFailureKind()` / `IsEntryCheckFailure()`.
- The event is process-wide: handlers in tests must be scoped (non-parallel test collection).
