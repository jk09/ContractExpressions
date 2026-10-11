# 0005. Ship the FsCheck integration as a separate package, ContractCheck.FsCheck

- Status: Accepted
- Date: 2026-10-11
- Features: –
- Spec: amber-harbor-v9p6ri

## Context
Contracts make good test oracles: random inputs that pass the preconditions must satisfy the postconditions and
invariants. ContractExpressions4.Check had a `DbcPropertyTest` helper for FsCheck. The core library should stay free
of test dependencies.

## Options considered
1. **Helper inside ContractCheck** – one package; drags FsCheck (and FSharp.Core) into every production app.
2. **Separate ContractCheck.FsCheck package** – production code depends only on ContractCheck; tests add the helper.

## Decision
Option 2: `ContractCheck.FsCheck` (FsCheck 3) with `DbcPropertyTest.Check`, `CheckAsync`, `CheckSequence` and
`CheckSequenceAsync`. Entry-check failures discard a case; postcondition, invariant and unexpected exceptions fail it.
The `dbc-property-tests` Claude skill and its init script build on it.

## Consequences
- Two packages to version together (same version number).
- FsCheck major versions need a matching ContractCheck.FsCheck release.
