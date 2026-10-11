---
applyTo: "ContractCheck/**, ContractCheck.FsCheck/**, ContractCheck.Samples/**"
---

# ContractCheck – Design-by-Contract for .NET

ContractCheck provides Design by Contract ([Wikipedia](https://en.wikipedia.org/wiki/Design_by_contract)) for .NET
interfaces: preconditions, postconditions and invariants written with the `System.Diagnostics.Contracts` API of the
legacy [Code Contracts](https://learn.microsoft.com/en-us/dotnet/framework/debug-trace-profile/code-contracts), checked
at run time by a `System.Reflection.DispatchProxy` instead of IL rewriting. The legacy sources are in the
[CodeContracts](../../CodeContracts/CodeContracts.sln) submodule; where behaviour had to be chosen, ContractCheck follows them.

## The API

- `Dbc.Def(selector, clauses…)` – contracts for an interface member; `Dbc.Def(x => Contract.Invariant(…), …)` – interface invariants.
- `Dbc.DefSetter(selector, clauses…)` – contracts for property and indexer setters (assigned `value` last).
- `Dbc.Make<T>(target)` – a proxy of interface `T` that checks the contracts; checks the invariants before returning.
- `ContractViolationException` (`Kind` is a `ContractFailureKind`, `Method`, `Condition`, …); `Requires<TEx>` throws `TEx`.
- `ContractCheck.FsCheck.DbcPropertyTest` – FsCheck properties: precondition failures discard a case, other failures fail it.

## Example (ContractCheck.Samples/BankAccount)

```csharp
#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.BankAccount;

[ContractClass(typeof(AccountContracts))]
public interface IAccount
{
    bool SupportsOverdraft { get; }
    float OverdraftLimit { get; }
    float Amount { get; }
    void Deposit(float deposit);
    void Withdraw(float withdrawAmount);
}

[ContractClassFor(typeof(IAccount))]
internal sealed class AccountContracts
{
    public AccountContracts()
    {
        Dbc.Def(static (IAccount x) => x.OverdraftLimit,
            static (IAccount x) => Contract.Ensures(Contract.Result<float>() >= 0));

        Dbc.Def(static (IAccount x, float deposit) => x.Deposit(deposit),
            static (IAccount x, float deposit) => Contract.Requires(deposit > 0.0f));

        Dbc.Def(static (IAccount x, float withdrawAmount) => x.Withdraw(withdrawAmount),
            static (IAccount x, float withdrawAmount) => Contract.Requires(withdrawAmount <= x.Amount + x.OverdraftLimit),
            static (IAccount x, float withdrawAmount) => Contract.Ensures(x.Amount == Contract.OldValue(x.Amount) - withdrawAmount));

        Dbc.Def(static (IAccount x) => Contract.Invariant(x.SupportsOverdraft ? x.OverdraftLimit > 0 : x.OverdraftLimit == 0),
            static (IAccount x) => Contract.Invariant(x.OverdraftLimit <= 1000),
            static (IAccount x) => Contract.Invariant(x.Amount > -x.OverdraftLimit));
    }
}

IAccount account = Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));
account.Withdraw(40);    // fine
account.Withdraw(500);   // ContractViolationException: Precondition failed: withdrawAmount <= x.Amount + x.OverdraftLimit (in IAccount.Withdraw)
```

## Property-based testing

```csharp
[Property]
public Property Deposit_keeps_the_contracts(float deposit) =>
    DbcPropertyTest.Check(() => Dbc.Make<IAccount>(new Account(100, true, 20)), account => account.Deposit(deposit));
```

`Check` / `CheckAsync` test one call per generated case; `CheckSequence` / `CheckSequenceAsync` run a generated sequence
of calls on one proxy, so invariants are checked across state changes. See `ContractCheck.Samples.PropertyTests`.

## Rules for changes in these folders

- Keep semantics aligned with CodeContracts and the spec's resolutions (README "Migration"); a behaviour change needs an ADR.
- Every public behaviour has a test in `ContractCheck.Tests`; keep XML docs complete (warnings are errors).
