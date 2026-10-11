# 0003. Check interface invariants at creation and after non-pure members only

- Status: Accepted
- Date: 2026-10-11
- Features: Enforcement
- Spec: amber-harbor-v9p6ri

## Context
ContractExpressions ignored interface invariants; ContractExpressions4 checked them at creation and on entry and exit of
every member, getters included. CodeContracts checked object invariants at the end of constructors and of public
non-pure methods, never on entry, and not after property getters or `[Pure]` methods.

## Options considered
1. **Entry and exit** – catches state corrupted outside the proxy; doubles the cost; differs from CodeContracts.
2. **Creation and exit of non-pure members (CodeContracts)** – the classic DbC rule; cheaper; getters don't re-check.

## Decision
Option 2. `Dbc.Make` stands in for the constructor; members marked `[Pure]` and property getters skip the check;
invariants defined inside a member's `Dbc.Def` are checked at that member's exit only. Postconditions run before
invariants.

## Consequences
- An implementation mutated behind the proxy's back is caught at the next non-pure call, not at the next call.
- `[Pure]` cannot be put on interfaces (CS0592), so purity is per member.
