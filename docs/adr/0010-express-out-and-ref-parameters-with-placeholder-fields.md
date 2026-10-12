# 0010. Express out and ref parameters with placeholder fields

- Status: Proposed
- Date: 2026-10-12
- Features: Definitions, Enforcement
- Spec: canyon-tundra-ymf1vj

## Context
Expression-tree lambdas can't declare `out` or `ref` parameters, so members such as `TryGetValue(key, out value)` had
no contracts. CodeContracts wrote `Contract.ValueAtReturn(out value)` against the real parameter, which a separate
lambda doesn't have.

## Options considered
1. **Leave them unsupported** – the common Try-pattern stays unchecked.
2. **Delegate clauses for such members** – no expression tree: no printed conditions, no definition-time validation,
   no analyzer.
3. **Placeholder fields** `Dbc.Out<T>.Value` / `Dbc.Ref<T>.Value` (and `Value2`, `Value3`), which C# accepts as
   `out`/`ref` arguments inside expression trees – fits the existing API; looks unusual, and is limited to three
   parameters of one type per member.

## Decision
Option 3. The selector passes a placeholder for each by-ref parameter; in clauses a `Ref` placeholder is the value
passed in (entry checks, `OldValue`) or the value at return (postconditions), an `Out` placeholder exists only in
`Ensures`, and `Contract.ValueAtReturn(out placeholder)` is accepted as a synonym. Misuse is a definition error
(and CCK0001/CCK0005 at compile time).

## Consequences
- The placeholders are public static fields that are never read at run time (CA2211 suppressed).
- `DbcPropertyTest.CheckCalls` skips by-ref members; they need explicit `Check` properties.
