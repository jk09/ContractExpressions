# 0004. Check postconditions of async members when the returned task completes

- Status: Accepted
- Date: 2026-10-11
- Features: Enforcement
- Spec: amber-harbor-v9p6ri

## Context
Both old projects checked async members synchronously: postconditions ran when the task was returned, before the work
was done, and `Contract.Result<T>()` could only mean the task. CodeContracts checked async postconditions on completion
and let `Result<T>()` mean the awaited value.

## Options considered
1. **Treat tasks as plain values** – simple; postconditions about the work are useless or wrong.
2. **Defer to task completion** – correct semantics; the proxy must wrap the returned task.

## Decision
Option 2 for `Task`, `Task<T>`, `ValueTask` and `ValueTask<T>`: preconditions run synchronously; `Ensures`, member and
interface invariants run when the task succeeds; `EnsuresOnThrow` covers synchronous throws, faulted tasks and cancelled
tasks (as `OperationCanceledException`); `Result<T>()` is the awaited value. Members with nothing deferred return the
original task.

## Consequences
- A failure surfaces when the caller awaits, as a faulted task with `ContractViolationException`.
- Wrapping costs one continuation per checked async call; wrapper delegates are compiled once per return type.
