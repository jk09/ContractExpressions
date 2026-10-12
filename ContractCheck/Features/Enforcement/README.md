# Enforcement

Wraps an implementation in a proxy (`Dbc.Make<T>`) that checks the registered contracts of the interface and its base interfaces on every call, at the configured checking level.

## Entry points
- `Dbc.Make<T>(T target)` – loads the contract classes, checks interface invariants, returns the proxy – `Dbc.Make.cs`; `Dbc.Make(Type, object)` and `Dbc.HasContracts(Type)` for interfaces known at run time – `Dbc.MakeNonGeneric.cs`
- `Dbc.CheckingLevel` (`ContractCheckingLevel`: `None` … `Full`) – which clause kinds proxies check; initial value from runtimeconfig – `Dbc.CheckingLevel.cs`, filtered in `MemberContracts.cs`
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
- Each checking level runs exactly its clause kinds; skipped kinds cost nothing (no old values, no creation invariants); a bad runtimeconfig value fails naming it – `CheckingLevelTests`
- Target exceptions keep their stack trace – `ProxyTests`

## Dependencies
- Features: Definitions (`ContractRegistry`, `CompiledClause`), Failures (`FailureReporter`)
- External: BCL only (`System.Reflection.DispatchProxy`)
- Config: runtimeconfig / `AppContext` setting `ContractCheck.CheckingLevel` (level name; default `Full`)

## Feature flags
| Flag | Default | Owner | Remove by |
|---|---|---|---|
| – | | | |

Spec: amber-harbor-v9p6ri, canyon-tundra-ymf1vj · ADRs: 0001, 0003, 0004, 0007
