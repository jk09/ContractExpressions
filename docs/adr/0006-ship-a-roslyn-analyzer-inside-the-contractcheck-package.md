# 0006. Ship a Roslyn analyzer inside the ContractCheck package

- Status: Accepted
- Date: 2026-10-12
- Features: Definitions
- Spec: canyon-tundra-ymf1vj

## Context
`Dbc.Def` mistakes only surfaced at the first `Dbc.Make`, as `InvalidOperationException`s. The `CONTRACTS_FULL`
pitfalls were silent: contract attributes vanish from files without the symbol, and `Contract.Requires` in ordinary
code terminates the process where the symbol is defined (`Requires<TException>` everywhere). A derived interface could
add preconditions to an inherited member unnoticed; the owner decided (2026-10-11) to warn about that, not reject it.

## Options considered
1. **Run-time validation only** – nothing new to maintain; every mistake waits for a test or a user to hit it.
2. **Separate analyzer package** – opt-in; most users would never install it.
3. **Analyzer inside the ContractCheck package** (`analyzers/dotnet/cs`), code fix in a second assembly – every user
   gets it with no setup; the definition rules exist twice, once over expression trees and once over syntax.

## Decision
Option 3. `ContractCheck.Analyzers` (Roslyn 4.8, so .NET 8 SDKs load it) reports CCK0001–CCK0005 with the run-time
messages, CCK0006 (attributes compiled away, code fix in `ContractCheck.CodeFixes`), CCK0007 (`Contract` calls that
terminate the process) and the warning CCK0008 (preconditions added through a derived interface). The run-time
validation stays authoritative and unchanged.

## Consequences
- A rule change needs both implementations; `RuntimeAgreementTests` fails when they disagree on this repository's tests.
- `ContractCheck.FsCheck` and `ContractCheck.DependencyInjection` pass the analyzer on to projects that only install them.
- Compilers older than Roslyn 4.8 skip the analyzer with a warning; ContractCheck needs .NET 8 anyway.
