# ContractCheck.Samples.PropertyTests

Property-based tests for **ContractCheck.Samples**: FsCheck generates inputs, `Dbc.Make` proxies check the ContractCheck
contracts, and `DbcPropertyTest` turns the outcome into a property (entry-check failures discard the case;
postcondition and invariant failures fail it).

- One `*Properties.cs` file per contract interface; custom generators go in `Generators.cs`.
- Written and maintained by the `dbc-property-tests` Claude skill (`.claude/skills/dbc-property-tests`).
- Run: `dotnet test ContractCheck.Samples.PropertyTests/ContractCheck.Samples.PropertyTests.csproj`

## Known defects (pinned)

The samples are ports of the CodeContracts and ContractExpressions4 demos; some contain contract defects – three of them
deliberate in the CodeContracts demos. Each is pinned by a `Known_defect_…` fact that asserts FsCheck falsifies the
property, so the suite stays green and the defect stays visible:

| Interface | Defect | Found by |
|---|---|---|
| `IAccount` | `Withdraw` admits withdrawing everything available, which breaks the strict invariant `Amount > -OverdraftLimit` | any valid account + full withdrawal |
| `ISimpleAccount` | preconditions admit `∞`; `Credit(∞)` then `Debit(∞)` leaves `NaN` | edge-value generator; also `DbcPropertyTest.CheckCalls` with no custom generator |
| `IMaxUtils` | `Max` of an empty array breaks the `Exists` postcondition | arrays, one in four empty |
| `IBinaryOperation` (`IntDiv`) | division by zero passes every contract check, then throws | divisors −2…2 |
