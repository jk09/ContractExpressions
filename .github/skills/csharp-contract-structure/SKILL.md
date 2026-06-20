---
name: csharp-contract-structure
description: 'Generate C# domain code using ContractExpressions4 with interface + implementation + contract class. Use when creating new C# classes and deriving reliable preconditions, postconditions, and invariants via Dbc.Def.'
argument-hint: 'Describe the class or feature to create with contracts'
---

# C# Contract Structure (ContractExpressions4)

Create C# code using a contract-first shape based on ContractExpressions4.

## When to Use
- Creating a new C# class or feature in this workspace.
- Refactoring class-based logic into interface-driven design with contracts.
- Generating tests that validate both valid behavior and contract violations.

## Required Output Shape
For each new domain concept, generate three files:
1. `{Name}.cs` with interface and implementation.
2. `{Name}.Contracts.cs` with `ContractClass` and `ContractClassFor` definitions and `Dbc.Def(...)` registration.
3. `{Name}.Tests.cs` with deterministic xUnit tests and FsCheck property tests.

If project constraints require fewer files, still preserve the interface + implementation + contract-class separation.

## Contract Rules
- Use `ContractExpressions4` APIs only:
  - `Dbc.Def(method, ...contracts)`
  - `Dbc.Make<TInterface>(instance)`
- Use `static` lambdas in every `Dbc.Def` call.
- Use single-argument `Dbc.Def(static (T t) => Contract.Invariant(...))` only for invariants.
- Add contracts only when they are reliably inferable from requirements.
- Do not create placeholder or speculative contract clauses.
- If you skip contracts for a member, explain why they were not inferable.

## Dbc.Def Coverage
Match the selector and clause signatures to the member arity:
- `void` members: `Action<T>` through `Action<T, T1, ..., T7>`.
- returning members: `Func<T, TResult>` through `Func<T, T1, ..., T7, TResult>`.

## Procedure
1. Parse the request into interface members (methods and properties).
2. Implement the concrete class behavior.
3. Add contracts in the contract class constructor with `Dbc.Def(...)` only.
4. Prefer preconditions for input validity, postconditions for return/state guarantees, and invariants for persistent object truths.
5. Generate deterministic tests for success and failure paths of each defined contract.
6. Generate FsCheck property tests using `ContractExpressions4.Check.DbcPropertyTest.Check`.

## Test Expectations
- Include satisfied-path tests for each contracted member.
- Include violated-path tests for each defined contract kind.
- Verify `ContractViolationException.Kind` where relevant.
- For creation-time invariant checks, assert method is `"<creation>"` when applicable.

## Notes
- Start files that use `System.Diagnostics.Contracts` with `#define CONTRACTS_FULL`.
- Keep contract constructors free of non-`Dbc.Def(...)` logic.
- Prefer minimal, non-speculative code and align with existing project style.
