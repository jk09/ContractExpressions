# Failures

Reports contract failures the way CodeContracts did: raise `Contract.ContractFailed`, then throw `ContractViolationException` (or the `Requires<TException>` exception) unless a handler marked the failure handled.

## Entry points
- `ContractViolationException` – public; `Kind`, `Method`, `Condition`, `UserMessage`, `ContractText`, `InterfaceType`, `Member` – `ContractViolationException.cs`
- `GetContractFailureKind()` / `IsEntryCheckFailure()` extensions – `ContractFailureExtensions.cs`
- `FailureReporter.Report` – raises the event and decides handled / throw – `FailureReporter.cs`
- `TypedExceptionFactory` – builds `Requires<TException>` exceptions with the legacy constructor rules – `TypedExceptionFactory.cs`

## Invariants
- Every failure raises `Contract.ContractFailed`; `SetHandled()` lets the call continue; `SetUnwind()` or a throwing handler still ends in `ContractViolationException` – `ContractFailedEventTests`
- `Requires<TException>` always throws `TException`; a `(paramName, message)` constructor receives the user message as `ParamName` – `TypedRequiresTests`
- Messages use the BCL format plus the member: `Precondition failed: value > 0  msg (in IAccount.Withdraw)` – `ContractViolationExceptionTests`
- `IsEntryCheckFailure()` is true exactly for preconditions (typed too), `Assert` and `Assume` – `ContractViolationExceptionTests`, `TypedRequiresTests`

## Dependencies
- Features: none
- External: BCL only (`System.Diagnostics.Contracts.Internal.ContractHelper`)
- Config: none

## Feature flags
| Flag | Default | Owner | Remove by |
|---|---|---|---|
| – | | | |

Spec: amber-harbor-v9p6ri · ADRs: 0002
