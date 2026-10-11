# Enforcement

Wraps an implementation in a proxy (`Dbc.Make<T>`) that checks the registered contracts of the interface and its base interfaces on every call.

## Entry points
- `Dbc.Make<T>(T target)` – loads the contract classes, checks interface invariants, returns the proxy – `Dbc.Make.cs`
- `ContractAwareProxy` (`DispatchProxy`) → `ContractInvoker.Invoke` – runs the checks around each call – `ContractInvoker.cs`
- `ContractClassLoader` – runs each contract class once (thread-safe), validates `[ContractClassFor]`, closes generic contract classes; falls back to the `[ContractClassFor]` class when a Release build dropped `[ContractClass]` – `ContractClassLoader.cs`
- `AsyncContracts` – defers postconditions and invariants of `Task`/`ValueTask` members to task completion – `AsyncContracts.cs`

## Invariants
- Order per call: entry checks → old values → target → `EnsuresOnThrow` (on throw) → `Ensures` → member invariants → interface invariants – `EvaluationOrderTests`
- Interface invariants are checked at creation and after non-pure members, never on entry, not after getters or `[Pure]` members – `InvariantTests`
- Contracts come from the proxied interface and its base interfaces (base first), never from siblings – `InheritanceTests`
- Async members: postconditions and invariants run when the task succeeds; `EnsuresOnThrow` covers faulted and cancelled tasks – `AsyncTests`
- `[ContractOption]` switches off runtime checking or inheritance; `[ContractRuntimeIgnored]` clauses are skipped – `ContractOptionTests`
- Contracts load the same in Debug and Release, even when `[ContractClass]` was compiled away – `ContractClassLoadingTests`
- Target exceptions keep their stack trace – `ProxyTests`

## Dependencies
- Features: Definitions (`ContractRegistry`, `CompiledClause`), Failures (`FailureReporter`)
- External: BCL only (`System.Reflection.DispatchProxy`)
- Config: none

## Feature flags
| Flag | Default | Owner | Remove by |
|---|---|---|---|
| – | | | |

Spec: amber-harbor-v9p6ri · ADRs: 0001, 0003, 0004
