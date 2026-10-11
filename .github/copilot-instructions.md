# Project Guidelines

## Architecture

ContractCheck is a runtime Design-by-Contract library for .NET 8, 9 and 10. It keeps the `System.Diagnostics.Contracts`
vocabulary of the discontinued CodeContracts but replaces IL rewriting with expression trees and `DispatchProxy`.

**Core flow:** contracts are `Dbc.Def` / `Dbc.DefSetter` lambdas in the constructor of a `[ContractClassFor]` class →
validated and compiled once (`ContractCheck/Features/Definitions`) → stored per interface member in `ContractRegistry` →
`Dbc.Make<T>(target)` loads the contract classes of `T` and its base interfaces and returns a proxy that checks them on
every call (`ContractCheck/Features/Enforcement`) → failures raise `Contract.ContractFailed`, then throw
`ContractViolationException` (`ContractCheck/Features/Failures`).

**Order per call:** entry checks (`Requires`, `Requires<TEx>`, `Assert`, `Assume`) → old values → target →
`EnsuresOnThrow` (if it threw) → `Ensures` → member invariants → interface invariants (not after getters / `[Pure]`).
Async members: postconditions and invariants run when the task completes.

Public API: `Dbc.Def`, `Dbc.DefSetter`, `Dbc.Make<T>`, `ContractViolationException`, `GetContractFailureKind()` /
`IsEntryCheckFailure()`; `ContractCheck.FsCheck` adds `DbcPropertyTest`. Everything else is `internal`.

## Code Style

- Latest C#, file-scoped namespaces, nullable enabled, implicit usings, `TreatWarningsAsErrors`, XML docs on public API
- One feature per folder under `ContractCheck/Features/`; new types `internal` unless they extend the public API
- Contract definitions use `static` lambdas; files that call `Contract.*` or carry contract attributes (`[ContractClass]`, `[Pure]`) start with `#define CONTRACTS_FULL` – never define it project-wide

## Build and Test

```bash
dotnet build ContractExpressions.sln
dotnet test ContractExpressions.sln
dotnet test ContractCheck.Tests --filter "FullyQualifiedName~Invariant"   # subset
```

## Project Conventions

```csharp
[ContractClassFor(typeof(IMyList))]
internal sealed class MyListContracts
{
    public MyListContracts()
    {
        Dbc.Def(static (IMyList x, object a) => x.Add(a),
            static (IMyList x, object a) => Contract.Requires(a != null),
            static (IMyList x, object a) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) + 1));

        Dbc.Def(static (IMyList x) => Contract.Invariant(x.Count >= 0));
    }
}
```

- Tests (`ContractCheck.Tests`, xUnit v3) are self-contained per file: interfaces, implementations and contract classes
  live in the test file. Tests that subscribe `Contract.ContractFailed` join the non-parallel `ContractFailedHandlers`
  collection. Ported tests name their origin (`…_ported_from_<legacy test>`).
- `Dbc.Def` overloads cover 0–15 parameters (void and value-returning); keep `Dbc.Def.cs` / `Dbc.DefSetter.cs` uniform.

## Key Files

| File | Role |
|------|------|
| `ContractCheck/Features/Definitions/Dbc.Def.cs`, `Dbc.DefSetter.cs` | Public `Def` / `DefSetter` overloads |
| `ContractCheck/Features/Definitions/ClauseCompiler.cs` | Validates and compiles clauses (`Result`, `OldValue`, quantifiers) |
| `ContractCheck/Features/Enforcement/ContractInvoker.cs` | Runs the checks around each proxied call |
| `ContractCheck/Features/Enforcement/ContractClassLoader.cs` | Loads `[ContractClass]` classes once per interface |
| `ContractCheck/Features/Failures/FailureReporter.cs` | `ContractFailed` event + exceptions |
| `ContractCheck.FsCheck/DbcPropertyTest.cs` | FsCheck properties with contracts as the oracle |
| `docs/specs/`, `docs/adr/`, `docs/features.md` | Specs, decisions, feature index |
| `CodeContracts/` | Git submodule – original Microsoft CodeContracts (reference only) |
