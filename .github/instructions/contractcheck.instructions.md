---
applyTo: "ContractExpressions4/**, ContractExpressions4.Check/**"
---

# `ContractExpressions` — Design-by-Contract for .NET

## Overview

The `ContractExpressions` project provides the Design by Contract (DbC) functionality in .NET. 

The principles of DbC are described in the [Wikipedia article](https://en.wikipedia.org/wiki/Design_by_contract). In short, DbC is a programming methodology that prescribes that software designers should define formal, precise and verifiable interface specifications for software components, which extend the ordinary definition of abstract data types with preconditions, postconditions and invariants.

The `ContractExpressions` project is a lightweight replacement of the legacy project [Code contracts](https://learn.microsoft.com/en-us/dotnet/framework/debug-trace-profile/code-contracts), and uses its API in the namespace `System.Diagnostics.Contracts`. However, instead of the compile-time rewriting it utilizes the .NET `System.Reflection.DispatchProxy` class to define contracts on a per-interface basis using method call interception. The referemnce source code of the legacy Code Contracts framework is available in the [CodeContracts](../../CodeContracts/CodeContracts.sln) solution.


## The API

The API of the `ContractExpressions` project consists of two public static methods:

- `Dbc.Make<T>(target)` — creates a proxy for the interface `T` implemented by the `target` object that enforces contracts on every method call.
- `Dbc.Def(...)` — defines contracts for the interface `T` 

### Example

The following example shows how to define contracts for the interface `IAccount` and how to create a proxy for the implementation `Account` (with basic deposit and withdrawal functionality) that enforces the contracts.

The class `Account` is defined in the file `Account.cs` as follows:

```csharp

namespace ContractExpressions4.BankAccount;

internal sealed class Account(float openingAmount, bool supportsOverdraft = false, float overdraftLimit = 0) : IAccount
{
    private float amount = openingAmount;
    private readonly bool supportsOverdraftFlag = supportsOverdraft;
    private readonly float overdraftLimitValue = overdraftLimit;

    public bool SupportsOverdraft => supportsOverdraftFlag;
    public float OverdraftLimit => overdraftLimitValue;
    public float Amount => amount;

    public void Deposit(float deposit)
    {
        amount += deposit;
    }

    public void Withdraw(float withdrawAmount)
    {
        amount -= withdrawAmount;
    }
}
```


The contract interface `IAccount` and the contracts for the interface `IAccount` are defined in the file `Account.Contracts.cs` as follows:

```csharp
#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.BankAccount;

[ContractClass(typeof(AccountContracts))]
internal interface IAccount
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
            static (IAccount x, float withdrawAmount) => Contract.Ensures(x.Amount == Contract.OldValue<float>(x.Amount) - withdrawAmount));

        Dbc.Def(static (IAccount x) => Contract.Invariant(x.SupportsOverdraft ? x.OverdraftLimit > 0 : x.OverdraftLimit == 0));
        Dbc.Def(static (IAccount x) => Contract.Invariant(x.OverdraftLimit <= 1000));
        Dbc.Def(static (IAccount x) => Contract.Invariant(x.Amount > -x.OverdraftLimit));
    }
}
```

The code below shows how to create a proxy for the `Account` class that enforces the contracts defined in the `AccountContracts` class:

```csharp
IAccount proxy = Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));

proxy.Withdraw(40);
```

If the `Withdraw` method is called with an amount that exceeds the current balance plus the overdraft limit, a contract violation exception will be thrown, indicating that the precondition has been violated.


## Property-based testing

The `ContractExpressions4.Check` project provides property-based testing functionality for the `ContractExpressions` project. It uses the `FsCheck` library to generate random test cases and verify that the contracts are satisfied. The goal of property-based testing is to test the contracts with a wide range of inputs, including edge cases, to ensure that the contracts are robust and reliable. The API of the `ContractExpressions4.Check` project consists of a public static methods:

```cs
public static class DbcPropertyTest
{
    // For methods with a return value (the return value itself is not checked here;
    // contract correctness is asserted by the proxy's postcondition evaluation).
    public static Property Check<T, TResult>(Func<T> proxyFactory, Func<T, TResult> invoke)
        where T : class
        => RunCheck(proxyFactory, proxy => { invoke(proxy); });
    
    // For void methods.
    public static Property Check<T>(Func<T> proxyFactory, Action<T> invoke)
        where T : class
        => RunCheck(proxyFactory, invoke);

    // ...
}
```

The `Check<T, TResult>` and `Check<T>` methods  are to be used in tests which verify that the contracts are satisfied for randomly generated inputs. The `proxyFactory` function is used to create a proxy for the interface `T` that enforces the contracts, and the `invoke` function is used to call the method being tested on the proxy. The `Check` methods will generate random inputs and verify that the contracts are satisfied for each input.

### Example

```cs
namespace ContractExpressions4.Tests.Demo.BankAccount;

public class BankAccountDemoTests
{
    [Fact]
    public void OverdraftLimit_GetterContract_IsEnforced()
    {
        IAccount proxy = Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));

        float limit = proxy.OverdraftLimit;

        Assert.Equal(20, limit);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Deposit_RandomInputs_SatisfyContracts(float deposit) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20)),
            (IAccount proxy) => proxy.Deposit(deposit));
}
```