# 0001. Fuse ContractExpressions and ContractExpressions4 into one library, ContractCheck

- Status: Proposed
- Date: 2026-10-11
- Features: Definitions, Enforcement, Failures
- Spec: amber-harbor-v9p6ri

## Context
The repository had two implementations of the same `Dbc.Def` / `Dbc.Make` API: ContractExpressions (hand-written) and
ContractExpressions4 (AI-written), plus near-duplicate test suites. A side-by-side probe found 21 points (R1–R21 in the
spec) where they differ from each other or from CodeContracts – failure model, invariant timing, `Requires<TException>`,
`OldValue`, inheritance, … Each covered only part of `System.Diagnostics.Contracts`, and ContractExpressions4 failed 11
of its own 133 tests.

## Options considered
1. **Keep one implementation, delete the other** – least work; inherits that project's gaps and its side of every conflict.
2. **Fuse into a new library, resolving each conflict explicitly** – more work; one behaviour per conflict, chosen on purpose.

## Decision
Option 2: a new `ContractCheck` library (`net8.0;net9.0;net10.0`) that keeps the `Dbc.Def`/`Dbc.Make` API of both and
resolves every conflict with legacy CodeContracts (the `CodeContracts` submodule: Foxtrot rewriter and its tests) as the
tie-breaker, so code and knowledge from CodeContracts carry over. Both old projects and their tests are deleted; their
tests are ported (names say where each came from).

## Consequences
- One API, one test suite (ports + CodeContracts ports); migration notes R1–R21 in the README.
- Behaviour follows CodeContracts even where one old project was "friendlier" (e.g. invariants not checked on entry).
- Wrong if users depended on a resolved-away behaviour: the migration table is the place to look first.
