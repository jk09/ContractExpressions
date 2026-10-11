# ContractCheck.FsCheck

[FsCheck](https://fscheck.github.io/FsCheck/) property-based testing for [ContractCheck](../README.md): the contracts of an interface become the test oracle, so a property only has to say *which call to make* – the proxy checks the result.

```csharp
using ContractCheck;
using ContractCheck.FsCheck;
using FsCheck;
using FsCheck.Xunit;

public class AccountProperties
{
    [Property]
    public Property Withdraw_keeps_the_contracts(decimal amount) =>
        DbcPropertyTest.Check(() => Dbc.Make<IAccount>(new Account(100m)), account => account.Withdraw(amount));

    [Property]
    public Property Any_sequence_of_deposits_and_withdrawals_keeps_the_invariants(List<(bool Deposit, decimal Amount)> operations) =>
        DbcPropertyTest.CheckSequence(
            () => Dbc.Make<IAccount>(new Account(100m)),
            operations.Select(op => (Action<IAccount>)(account =>
            {
                if (op.Deposit) account.Deposit(op.Amount); else account.Withdraw(op.Amount);
            })));
}
```

| Outcome of a generated case | Result |
|---|---|
| Entry check fails (`Requires`, `Requires<TException>`, `Assert`, `Assume`) | Case discarded – the input is outside the contract |
| Postcondition, `EnsuresOnThrow` or invariant fails | Property fails; FsCheck shrinks the input |
| Interface invariant fails in `Dbc.Make` | Property fails |
| Any other exception | Property fails |

API: `Check` (awaits whatever the call returns: `Task`, `ValueTask`, `ValueTask<T>`), `CheckAsync` (`Task`-returning delegates and `async` lambdas), `CheckSequence` / `CheckSequenceAsync` (one proxy, many operations; steps rejected by an entry check are skipped).

If most generated inputs violate the preconditions, FsCheck gives up ("arguments exhausted"); constrain the generators instead, e.g. `PositiveInt`, `NonEmptyString`, `NonNull<T>` or `Gen.Choose`. FsCheck 3's defaults never generate `null` strings or `int.MinValue`: add edge values on purpose (`IntWithMinMax`, `Gen.OrNull`, `Gen.Frequency`).
