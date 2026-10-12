# Definitions

Turns the `Dbc.Def` / `Dbc.DefSetter` lambdas of a contract class into validated, compiled clauses, registered per interface member.

## Entry points
- `Dbc.Def` – members with 0–15 parameters, `void` and value-returning, plus the interface-invariant form – `Dbc.Def.cs`
- `Dbc.DefSetter` – property and indexer setters (0–14 index parameters, assigned `value` last) – `Dbc.DefSetter.cs`
- `Dbc.Out<T>` / `Dbc.Ref<T>` – placeholder fields that stand for `out`/`ref` parameters in selectors and clauses – `Dbc.Placeholders.cs`
- `ContractDefinitions.Define` – validates the selector (`MemberSelector.cs`), compiles every clause (`ClauseCompiler.cs`), then registers them together (`ContractRegistry.cs`); `ExpressionPrinter.cs` renders clause text for failure messages

## Invariants
- A selector is the outermost call of an interface member with the lambda's own parameters in order, and a placeholder for each `out`/`ref` parameter; anything else is a definition error – `SelectorTests`, `ByRefParameterTests`
- `Result` only in `Ensures` (matching type, non-`void` member); `OldValue` only in postconditions, never on a literal; `ValueAtReturn` only on a placeholder in `Ensures`; `EndContractBlock` is rejected – `ClauseValidationTests`
- A `ref` placeholder is the value passed in (entry checks, `OldValue`) or at return (postconditions); an `out` placeholder exists only in `Ensures` – `ByRefParameterTests`
- Every definition error a `Dbc.Def` call can show at compile time is also an analyzer error (`ContractCheck.Analyzers`, CCK0001–CCK0005), and nothing else is – `RuntimeAgreementTests`
- A `Def` call registers all of its clauses or none (all are compiled before any is registered) – `ClauseValidationTests.A_definition_with_an_invalid_clause_registers_none_of_its_clauses`
- Several `Def` calls for one member add up – `RepeatedDefinitionTests`
- Clause text in messages reads like the source, e.g. `x.Count == Contract.OldValue(x.Count) + 1` – `ExpressionPrinterTests`

## Dependencies
- Features: none (Enforcement reads the registry)
- External: BCL only (`System.Linq.Expressions`, `System.Diagnostics.Contracts`)
- Config: none

## Feature flags
| Flag | Default | Owner | Remove by |
|---|---|---|---|
| – | | | |

Spec: amber-harbor-v9p6ri, canyon-tundra-ymf1vj · ADRs: 0001, 0006, 0010
