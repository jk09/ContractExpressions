---
name: dbc-definition
description: When .NET classes and methods are created or modified, define contracts for them. The contracts are preconditions, postconditions and invariants in the Design-by-Contract (DBC) terminology. Verify the contracts with deterministic xUnit tests and FsCheck property tests.
license: MIT
compatibility: dotnet 9+, csharp, xunit, fscheck
argument-hint: 'Describe a new .NET class or method, and optionally define its contracts.'
---

# DBC API

The DBC implementation is defined in the project [`ContractExpressions4`](../../../ContractExpressions4/ContractExpressions4.csproj). The principles of DBC are in the
[README file](../../../README.md) and the
 [Design-by-Contract](https://en.wikipedia.org/wiki/Design_by_contract) Wikipedia article. The implementation is based on the .NET `System.Diagnostics.Contracts` namespace, but it does not use the legacy IL-rewriting approach. Instead, it uses the `System.Reflection.DispatchProxy` class to intercept method calls on interfaces and evaluate contract expressions at runtime.

The public API surface is limited to two methods: `Dbc.Def(...)` for registering contracts and `Dbc.Make<TInterface>(instance)` for creating a contract-aware proxy.
The contracts are defined in a separate contract class using the `Dbc.Def(...)` method. 

# DBC API Example


## Class definition
```csharp
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

## Contract interface definition

```csharp
#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.BankAccount;

[ContractClass(typeof(AccountContracts))]
internal interface IAccount
{
    bool SupportsOverdraft { get; }
    float OverdraftLimit { get; }
    float Amount { get; }

    void Deposit(float deposit);
    void Withdraw(float withdrawAmount);
}
```

## Contract class definition
```csharp
#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.BankAccount;

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

## Contract proxy usage and test with xUnit and FxCheck

- fact testing
- property testing

```csharp
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

# When to Use

## When a class is created

Let a new .NET class `MyClass` be created in the file `MyClass.cs`. Then 

1. define a new file `MyClass.Contracts.cs` with 
    - the contract interface interface `IMyClass` that is annotated with `[ContractClass(typeof(MyClassContracts))]` and contains the public members of `MyClass`.
    - the contract class `MyClassContracts` that is annotated with `[ContractClassFor(typeof(MyClass))]` and contains a default constructor that calls `Dbc.Def(...)` to register the contracts for the class `MyClass`.
2. define a new file `MyClass.Tests.cs` with xUnit and FsCheck tests that verify the contracts for the class `MyClass`.

The contracts involve preconditions, postconditions and invariants for the public members of the class `MyClass`. The contracts are inferred from the class definition and the requirements of the class in the skill prompt. If the skill prompt contains explicit contract definitions, they are used in addition to the inferred contracts. 

## Required Output Shape
For each new domain concept, generate three files:
1. `{Name}.cs` with interface and implementation.
2. `{Name}.Contracts.cs` with `ContractClass` and `ContractClassFor` definitions and `Dbc.Def(...)` registration.
3. `{Name}.Tests.cs` with deterministic xUnit tests and FsCheck property tests.
if I
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
