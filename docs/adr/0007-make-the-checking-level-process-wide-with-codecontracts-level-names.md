# 0007. Make the checking level process-wide, with CodeContracts' level names

- Status: Accepted
- Date: 2026-10-12
- Features: Enforcement
- Spec: canyon-tundra-ymf1vj

## Context
Every proxy checked every clause on every call. Production often wants only the cheap checks (preconditions), and
operators want to change that without a rebuild. CodeContracts chose the level per assembly at rewrite time: Full,
Pre and Post, Preconditions, ReleaseRequires, None.

## Options considered
1. **Per-assembly or per-interface attributes** – fine-grained; a rebuild for every change, and attributes are
   `[Conditional("CONTRACTS_FULL")]`.
2. **One process-wide level, set in code or runtimeconfig** – one switch operators can edit in
   `<app>.runtimeconfig.json`; all proxies (and all tests in a process) share it.
3. **Environment variable** – unusual for .NET libraries; runtimeconfig already covers deployment-time settings.

## Decision
Option 2, chosen by the owner on 2026-10-11: `Dbc.CheckingLevel` (`ContractCheckingLevel` with CodeContracts' names),
initialised from the runtimeconfig/`AppContext` setting `ContractCheck.CheckingLevel`. Clause kinds outside the level
cost nothing; definitions are validated at every level; an unknown level name fails at first use, naming the value.

## Consequences
- Tests that change the level run in a non-parallel collection (`GlobalSettings`).
- `[ContractOption("runtime", "checking", false)]` stays the way to switch single interfaces or members off; finer
  levels would need a new ADR.
