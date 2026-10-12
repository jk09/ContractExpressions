# ContractCheck

[![CI](https://github.com/jk09/ContractExpressions/actions/workflows/ci.yml/badge.svg)](https://github.com/jk09/ContractExpressions/actions/workflows/ci.yml)

Design by Contract for .NET: preconditions, postconditions and invariants written with the familiar
`System.Diagnostics.Contracts` API and checked at run time on every interface call – no IL rewriting, no build step.

```csharp
Dbc.Def(static (IAccount a, decimal amount) => a.Withdraw(amount),
    static (IAccount a, decimal amount) => Contract.Requires(amount > 0),
    static (IAccount a, decimal amount) => Contract.Requires(amount <= a.Balance, "insufficient funds"),
    static (IAccount a, decimal amount) => Contract.Ensures(a.Balance == Contract.OldValue(a.Balance) - amount));

IAccount account = Dbc.Make<IAccount>(new Account(100));
account.Withdraw(500);
// ContractViolationException: Precondition failed: amount <= a.Balance  insufficient funds (in IAccount.Withdraw)
```

- **Familiar** – `Requires`, `Requires<TException>`, `Ensures`, `EnsuresOnThrow`, `Invariant`, `Result`, `OldValue`,
  `ForAll`, `Exists`, `Assert`, `Assume`, with the semantics of the original CodeContracts.
- **Runtime only** – a `DispatchProxy` checks each call; contracts are expression trees, validated and compiled once.
- **Checked while you type** – the package's Roslyn analyzer reports invalid contracts and `CONTRACTS_FULL` pitfalls.
- **Async-aware** – postconditions of `Task` and `ValueTask` members are checked when the task completes.
- **Tunable** – CodeContracts' checking levels, from `Full` down to `Preconditions` or `None`, set in runtimeconfig.
- **Testable** – `ContractCheck.FsCheck` turns contracts into property-based tests, down to generated call sequences,
  and a Claude Code skill infers contracts and writes those tests for you.

Targets .NET 8, 9 and 10. MIT licensed.

## Contents

- [Design by Contract in a nutshell](#design-by-contract-in-a-nutshell)
- [Getting started](#getting-started)
- [Writing contracts](#writing-contracts)
- [How contracts are checked](#how-contracts-are-checked)
- [Checking levels](#checking-levels)
- [Analyzer](#analyzer)
- [Dependency injection](#dependency-injection)
- [Coverage of System.Diagnostics.Contracts](#coverage-of-systemdiagnosticscontracts)
- [Property-based testing with FsCheck](#property-based-testing-with-fscheck)
- [Claude Code skill: infer contracts, verify them with property tests](#claude-code-skill-infer-contracts-verify-them-with-property-tests)
- [Limitations](#limitations)
- [Migrating from ContractExpressions and ContractExpressions4](#migrating-from-contractexpressions-and-contractexpressions4)
- [Repository layout, building, contributing](#repository-layout)

## Design by Contract in a nutshell

A *contract* is a predicate about the arguments and results of a method and about the state of the object. The
predicates are checked before and after calls; a predicate that is false means the contract is *violated*, which
points at a bug and an inconsistent program state. There are three kinds:

- **Preconditions** – what a caller must ensure before calling: an argument is positive, a string is 10–20 characters
  long, an index is within the bounds of an array argument, a buffer has room for the data. A failing precondition is a
  bug in the caller.
- **Postconditions** – what the method guarantees when it returns: the result is positive, `TryParse` returning `true`
  produced a value, a collection grew by exactly the number of added items. A failing postcondition is a bug in the
  method.
- **Invariants** – what is true of an object whenever no method is running: a sorted tree stays sorted, a widget's
  bounding box contains its content. Checked after the object is created and after every method that can change it.

Contracts on an *interface* are a shared safety net: every implementation is checked against the same specification.

## Getting started

### Install

```
dotnet add package ContractCheck                        # the library and its analyzer
dotnet add package ContractCheck.DependencyInjection    # optional: AddContractChecks for Microsoft.Extensions.DependencyInjection
dotnet add package ContractCheck.FsCheck                # test projects: property-based tests
```

Until the first release is on nuget.org, reference the projects of this repository instead
(`dotnet add reference path/to/ContractCheck/ContractCheck.csproj`, and for the analyzer a reference to
`ContractCheck.Analyzers.csproj` with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`).

### Define the contract of an interface

Contracts live in a *contract class*: `[ContractClass]` on the interface points to it, `[ContractClassFor]` points
back, and its parameterless constructor calls `Dbc.Def` once per member.

```csharp
#define CONTRACTS_FULL                      // see below

using System.Diagnostics.Contracts;
using ContractCheck;

[ContractClass(typeof(AccountContracts))]
public interface IAccount
{
    decimal Balance { get; }
    void Deposit(decimal amount);
    void Withdraw(decimal amount);
}

[ContractClassFor(typeof(IAccount))]
internal sealed class AccountContracts
{
    public AccountContracts()
    {
        Dbc.Def(static (IAccount a, decimal amount) => a.Deposit(amount),
            static (IAccount a, decimal amount) => Contract.Requires(amount > 0),
            static (IAccount a, decimal amount) => Contract.Ensures(a.Balance == Contract.OldValue(a.Balance) + amount));

        Dbc.Def(static (IAccount a, decimal amount) => a.Withdraw(amount),
            static (IAccount a, decimal amount) => Contract.Requires(amount > 0),
            static (IAccount a, decimal amount) => Contract.Requires(amount <= a.Balance, "insufficient funds"),
            static (IAccount a, decimal amount) => Contract.Ensures(a.Balance == Contract.OldValue(a.Balance) - amount));

        Dbc.Def(static (IAccount a) => Contract.Invariant(a.Balance >= 0));
    }
}
```

The first lambda of each `Dbc.Def` *selects* the member; the following lambdas take the same parameters and each make
one `Contract.*` call.

`#define CONTRACTS_FULL` must open every file that calls `Contract.*` – the methods are `[Conditional("CONTRACTS_FULL")]`,
and without the symbol the clause lambdas don't compile (CS0765). The contract attributes are conditional too: `[Pure]`,
`[ContractOption]` and `[ContractRuntimeIgnored]` only survive in files that define the symbol, and `[ContractClass]`
only there or in Debug builds – so define it in the interface's file as well; the [analyzer](#analyzer) warns (CCK0006)
and adds the line for you. If a Release build dropped `[ContractClass]`, `Dbc.Make` finds the contract class through
its `[ContractClassFor]`. Don't define `CONTRACTS_FULL` project-wide: without the old rewriter, `Contract.Requires`
calls in ordinary method bodies would then stop the process (`Environment.FailFast`), and `Contract.Requires<TException>`
does so even without the symbol (CCK0007).

### Check an implementation

```csharp
IAccount account = Dbc.Make<IAccount>(new Account(100));   // runs AccountContracts once, checks the invariant

account.Deposit(50);
account.Withdraw(-5);       // Precondition failed: amount > 0m (in IAccount.Withdraw)
Dbc.Make<IAccount>(new Account(-1));
                            // Invariant failed: a.Balance >= 0m (in IAccount, checked by Dbc.Make)
Dbc.Make<IAccount>(new BrokenAccount()).Withdraw(10);       // withdraws twice the amount
                            // Postcondition failed: a.Balance == Contract.OldValue(a.Balance) - amount (in IAccount.Withdraw)
```

Wrap implementations where they are created – a factory, the DI container
([`services.AddContractChecks()`](#dependency-injection)) or a test – and callers keep using the interface.
`Dbc.Make(typeof(IAccount), new Account(100))` does the same for an interface known only at run time, and
`Dbc.HasContracts(typeof(IAccount))` tells whether there is anything to check.

## Writing contracts

### Selecting members

| Member | Selector | Clause lambdas |
|---|---|---|
| method | `static (IFoo x, int a, string b) => x.Bar(a, b)` | `static (IFoo x, int a, string b) => …` |
| property getter | `static (IFoo x) => x.Count` | `static (IFoo x) => Contract.Ensures(Contract.Result<int>() >= 0)` |
| indexer getter | `static (IFoo x, int i) => x[i]` | `static (IFoo x, int i) => Contract.Requires(i >= 0)` |
| property setter | `Dbc.DefSetter(static (IFoo x) => x.Limit, …)` | `static (IFoo x, int value) => Contract.Requires(value >= 0)` |
| indexer setter | `Dbc.DefSetter(static (IFoo x, string k) => x[k], …)` | `static (IFoo x, string k, string value) => …` |
| `out`/`ref` parameters | `static (IFoo x, string k) => x.TryGet(k, out Dbc.Out<int>.Value)` | `static (IFoo x, string k) => …` – see below |

`Dbc.Def` has overloads for members with 0–15 parameters, `DefSetter` for 0–14 index parameters. The selector must be
a single call of an interface member with the lambda's parameters in order; anything else (nested calls, constants,
static or extension methods) is rejected with an explanation. Several `Dbc.Def` calls for the same member add up.

### `out` and `ref` parameters

Expression-tree lambdas can't declare by-ref parameters, so the selector passes a placeholder field and the clauses read it:

```csharp
Dbc.Def(static (ICache c, string key) => c.TryGet(key, out Dbc.Out<int>.Value),
    static (ICache c, string key) => Contract.Ensures(!Contract.Result<bool>() || Dbc.Out<int>.Value > 0));

Dbc.Def(static (ICounter c, int step) => c.Bump(ref Dbc.Ref<int>.Value, step),
    static (ICounter c, int step) => Contract.Requires(Dbc.Ref<int>.Value >= 0),          // the value passed in
    static (ICounter c, int step) => Contract.Ensures(Dbc.Ref<int>.Value == Contract.OldValue(Dbc.Ref<int>.Value) + step));
```

- `Dbc.Out<T>.Value` stands for an `out` parameter of type `T`, `Dbc.Ref<T>.Value` for a `ref` parameter; use `Value2`
  and `Value3` for more parameters of one type. `in` parameters take the lambda's own parameters, like any other.
- A `ref` placeholder is the value passed in when read in entry checks or inside `OldValue`, and the value at return in
  postconditions; an `out` placeholder only has a value in `Ensures`. `Contract.ValueAtReturn(out Dbc.Out<int>.Value)`
  is accepted as a synonym, as in CodeContracts.

### Preconditions

```csharp
Contract.Requires(index >= 0 && index < x.Count)
Contract.Requires(index < x.Count, "index out of range")                    // message: any string expression
Contract.Requires<ArgumentNullException>(name != null, nameof(name))       // throws ArgumentNullException, ParamName "name"
Contract.Assert(x.IsOpen)                                                   // Assert / Assume: entry checks of their own kinds
```

`Requires<TException>` keeps the legacy CodeContracts rules: it always throws `TException`; an exception type with a
`(paramName, message)` constructor (`ArgumentNullException`, `ArgumentOutOfRangeException`) receives the user message
as `ParamName`, others receive the failure message.

### Postconditions

```csharp
Contract.Ensures(Contract.Result<int>() >= 0)                               // the return value
Contract.Ensures(x.Count == Contract.OldValue(x.Count) + 1)                // a value captured on entry
Contract.Ensures(Contract.ForAll(Contract.Result<int[]>(), v => v >= 0))   // quantifiers
Contract.Ensures(Contract.ForAll(0, xs.Length, i => xs[i] == Contract.OldValue(xs[i]) * 2))   // old values per index
Contract.EnsuresOnThrow<InvalidOperationException>(x.Count == Contract.OldValue(x.Count))      // when it throws
```

`OldValue` takes any non-literal expression; it is captured after the preconditions hold, and a capture that throws
only matters if the clause actually reads it. `EnsuresOnThrow<TException>` applies when the member throws `TException`
or a subtype; the original exception then propagates (or becomes the inner exception of the contract failure).

### Invariants

```csharp
Dbc.Def(static (IStack s) => Contract.Invariant(s.Count >= 0),
    static (IStack s) => Contract.Invariant(s.IsEmpty == (s.Count == 0)));   // interface invariants

Dbc.Def(static (IStack s, string item) => s.Push(item),
    static (IStack s, string item) => Contract.Invariant(s.Count > 0));      // checked after Push only
```

Interface invariants are checked by `Dbc.Make` and after every member except property getters and methods marked
`[Pure]` – never on entry, and not when the member throws.

### Async members

For members returning `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`, preconditions run before the call;
postconditions and invariants run when the task completes, and `Contract.Result<T>()` is the awaited value:

```csharp
Dbc.Def(static (IInventory x, string sku, int quantity) => x.ReserveAsync(sku, quantity),
    static (IInventory x, string sku, int quantity) => Contract.Requires(quantity > 0),
    static (IInventory x, string sku, int quantity) => Contract.Ensures(Contract.Result<int>() == x.Available(sku)),
    static (IInventory x, string sku, int quantity) => Contract.EnsuresOnThrow<InvalidOperationException>(
        x.Available(sku) == Contract.OldValue(x.Available(sku)), "a failed reservation must not change the stock"));
```

A failed postcondition faults the returned task with `ContractViolationException`. `EnsuresOnThrow` also covers faulted
tasks and cancellation (as `OperationCanceledException`).

### Inheritance and generic interfaces

A proxy of `IDerived` checks the contracts of `IDerived` and of all its base interfaces (each from its own contract
class); contracts never leak to sibling interfaces. A generic interface may have a generic contract class –
`[ContractClass(typeof(StackContracts<>))]` – which is closed over the interface's type arguments.

### Switching checks off

- `[ContractOption("runtime", "checking", false)]` on an interface, a member or an assembly: no runtime checks.
- `[ContractOption("contract", "inheritance", false)]` on an interface: base-interface contracts don't apply.
- `[ContractRuntimeIgnored]` on a method or property: clauses that use it are not checked at run time.
- `Dbc.CheckingLevel` or the runtimeconfig setting `ContractCheck.CheckingLevel`: which kinds of clauses are checked
  at all – see [Checking levels](#checking-levels).

## How contracts are checked

- **When contracts are defined.** `Dbc.Make<T>` runs the contract class of `T` and of each base interface once
  (thread-safe). Every clause is validated and compiled then: a selector that isn't an interface member, `Result` outside
  `Ensures` or of the wrong type, `OldValue` outside a postcondition, `ValueAtReturn` without a placeholder,
  `EndContractBlock` … throw `InvalidOperationException` naming the clause and the reason, at every checking level; the
  [analyzer](#analyzer) reports the same mistakes at compile time. A `Dbc.Def` call registers all its clauses or none.
- **Order of a call.** Entry checks (`Requires`, `Requires<TException>`, `Assert`, `Assume`, in definition order) → old
  values → the target → `EnsuresOnThrow` if it threw → `Ensures` → member invariants → interface invariants.
- **Failures.** Every failure raises `Contract.ContractFailed`. If a handler calls `e.SetHandled()`, the call continues;
  otherwise a `ContractViolationException` is thrown with `Kind` (`ContractFailureKind`), `Method`, `Condition`,
  `UserMessage`, `ContractText`, `InterfaceType` and `Member`. `ex.GetContractFailureKind()` and
  `ex.IsEntryCheckFailure()` also recognise `Requires<TException>` exceptions.
- **Messages** follow the .NET format plus the member: `Precondition failed: amount <= a.Balance  insufficient funds
  (in IAccount.Withdraw)`. Conditions are printed from the expression tree, so they read like the source.
- **The target** is called with its exceptions and stack traces intact; members without contracts are forwarded.

## Checking levels

CodeContracts' runtime checking levels choose which clauses the proxies check – for example only preconditions in
production:

| `ContractCheckingLevel` | Checks |
|---|---|
| `Full` (default) | everything |
| `PreAndPost` | `Requires`, `Requires<TException>`, `Ensures`, `EnsuresOnThrow` – no invariants, `Assert` or `Assume` |
| `Preconditions` | `Requires`, `Requires<TException>` |
| `ReleaseRequires` | `Requires<TException>` only |
| `None` | nothing: calls are forwarded |

Set it in the app's project file – it lands in `<app>.runtimeconfig.json`, where operators can change it without a
rebuild:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="ContractCheck.CheckingLevel" Value="Preconditions" />
</ItemGroup>
```

or in code, `Dbc.CheckingLevel = ContractCheckingLevel.Preconditions;`, which applies from the next call of every proxy.
Skipped clauses cost nothing – no old values are captured while postconditions are off – and definitions are validated
at every level. An unknown level name in runtimeconfig makes the first `Dbc.Make` throw, naming the value.

## Analyzer

The `ContractCheck` package includes a Roslyn analyzer (for .NET 8 SDKs and newer) that applies the rules of `Dbc.Make`
while you type and catches the `CONTRACTS_FULL` pitfalls:

| ID | Severity | Reports |
|---|---|---|
| CCK0001 | Error | A selector `Dbc.Make` would reject: not an interface, not a member called on the first parameter, arguments that aren't the lambda's parameters in order, a by-ref parameter without its `Dbc.Out`/`Dbc.Ref` placeholder, `DefSetter` without a setter |
| CCK0002 | Error | A clause that isn't one `Contract.Requires`/`Ensures`/`EnsuresOnThrow`/`Invariant`/`Assert`/`Assume` call; another kind in the interface-invariant form; `Requires<TException>` with an exception it can't create |
| CCK0003 | Error | `Contract.Result<T>()` outside `Ensures`, on a `void` member, of the wrong type, or both the task and its awaited result in one clause |
| CCK0004 | Error | `Contract.OldValue` outside postconditions, of a literal, of `Result`, or of a lambda variable other than a `ForAll`/`Exists` index |
| CCK0005 | Error | An `out`/`ref` placeholder read where it has no value or that the selector doesn't pass; `ValueAtReturn` misuse; `EndContractBlock` as a clause |
| CCK0006 | Warning | `[Pure]`, `[ContractOption]`, `[ContractRuntimeIgnored]`, `[ContractClass]` or `[ContractClassFor]` in a file without `#define CONTRACTS_FULL`, where the compiler drops it. Code fix: add the define |
| CCK0007 | Error | `Contract.Requires`, `Ensures`, `EnsuresOnThrow` or `Invariant` in ordinary code of a file that defines `CONTRACTS_FULL`, and `Requires<TException>` outside a clause anywhere: without the CodeContracts rewriter they terminate the process |
| CCK0008 | Warning | A precondition (`Requires`, `Assert`, `Assume`) defined through a derived interface for an inherited member: code that holds the base interface can't know it (Liskov substitution; CodeContracts error CC1033) |

The errors are the exceptions `Dbc.Make` would throw, with the same texts, so a contract class that compiles cleanly
also loads. Change severities in `.editorconfig` as usual, e.g. `dotnet_diagnostic.CCK0008.severity = none`.

## Dependency injection

`ContractCheck.DependencyInjection` wraps existing Microsoft.Extensions.DependencyInjection registrations in proxies:

```csharp
services.AddScoped<IAccount, Account>();
services.AddContractChecks<IAccount>();   // or services.AddContractChecks(): every registered interface with contracts
```

Lifetimes and keys stay; type, factory, instance and keyed registrations are wrapped, and the container still disposes
each implementation exactly once. Details in the [package README](ContractCheck.DependencyInjection/README.md).

## Coverage of System.Diagnostics.Contracts

| Member | Support |
|---|---|
| `Requires(bool[, string])` | Entry check, kind `Precondition` |
| `Requires<TException>(bool[, string])` | Entry check; throws `TException` (legacy constructor rules) |
| `Ensures(bool[, string])` | Exit check, kind `Postcondition`; async: on task completion |
| `EnsuresOnThrow<TException>(bool[, string])` | When `TException` (or a subtype) is thrown, kind `PostconditionOnException` |
| `Invariant(bool[, string])` | Interface invariants, or member-scoped inside a member's `Dbc.Def` |
| `Assert(bool[, string])`, `Assume(bool[, string])` | Entry checks, kinds `Assert` / `Assume` |
| `Result<T>()` | In `Ensures`; the return value or the awaited value |
| `OldValue<T>(T)` | In `Ensures` / `EnsuresOnThrow`; any non-literal expression, also per index in `ForAll`/`Exists` |
| `ForAll`, `Exists` (collection and index-range forms) | In any condition |
| `ContractFailed` event, `ContractFailedEventArgs.SetHandled` / `SetUnwind` | Raised for every failure |
| `ContractFailureKind` | Reported for every failure |
| `[ContractClass]`, `[ContractClassFor]` | Contract-class discovery and validation, generic contract classes |
| `[Pure]` | Pure members skip the interface-invariant check (property getters always do) |
| `[ContractRuntimeIgnored]`, `[ContractOption]` | See [Switching checks off](#switching-checks-off) |
| `ValueAtReturn<T>(out T)` | In `Ensures`, on an [`out`/`ref` placeholder](#out-and-ref-parameters) |
| `EndContractBlock()` | Rejected with an explanation: write `Requires<TException>` |
| `[ContractInvariantMethod]` | Not needed: invariants are `Dbc.Def(x => Contract.Invariant(…))` |
| `[ContractVerification]`, `[ContractPublicPropertyName]`, `[ContractArgumentValidator]`, `[ContractAbbreviator]` | Static-checker and rewriter attributes – no runtime meaning |

## Property-based testing with FsCheck

Contracts make good test oracles: any input that passes the preconditions must satisfy the postconditions and
invariants. `ContractCheck.FsCheck` (FsCheck 3) builds that into properties:

```csharp
[Property]
public Property Withdraw_keeps_the_contracts(decimal amount) =>
    DbcPropertyTest.Check(() => Dbc.Make<IAccount>(new Account(100)), account => account.Withdraw(amount));

[Property(Arbitrary = [typeof(Generators)])]
public Property Deposits_and_withdrawals_keep_the_contracts(AccountStep[] steps) =>
    DbcPropertyTest.CheckSequence(() => Dbc.Make<IAccount>(new Account(100)), steps.Select(s => (Action<IAccount>)s.Apply));

[Property]
public Property Any_calls_keep_the_contracts() =>
    DbcPropertyTest.CheckCalls(() => Dbc.Make<IAccount>(new Account(100)));
```

- An entry-check failure (precondition, `Assert`, `Assume`) discards the generated case – the input was outside the
  contract. A postcondition or invariant failure, or any other exception, fails the property and FsCheck shrinks the
  input to a minimal counterexample.
- `Check` awaits returned tasks; `CheckAsync` takes `async` lambdas. `CheckSequence` / `CheckSequenceAsync` run a
  generated sequence of calls on one proxy, so invariants are checked across state changes (rejected steps are skipped).
- `CheckCalls` generates the sequences itself: random calls of every method, property and indexer of the interface,
  with arguments from FsCheck, shrunk to a call log such as `[Credit(Infinity), Debit(Infinity)]`. `CallOptions` adds
  generators (`ArbMap`), documented exceptions (`ExpectedExceptions`) and exclusions (`Exclude`).

[`ContractCheck.Samples.PropertyTests`](ContractCheck.Samples.PropertyTests) shows generators, sequences, async members,
and four contract defects in the sample programs that the properties find.

## Claude Code skill: infer contracts, verify them with property tests

[`.claude/skills/dbc-property-tests`](.claude/skills/dbc-property-tests/SKILL.md) is a [Claude Code](https://claude.com/claude-code)
skill for coding tasks. For the code changed in a task it

1. creates (once) a separate `<Project>.PropertyTests` project with xUnit v3, FsCheck and ContractCheck –
   `pwsh .claude/skills/dbc-property-tests/scripts/init-property-tests.ps1 -ProjectUnderTest src/Shop/Shop.csproj`;
2. infers contracts from guards, documentation, tests and callers, with evidence and a confidence level, and adds only
   high-confidence clauses next to the existing ones;
3. writes FsCheck properties for them, runs them and classifies every failure;
4. runs the existing tests and reports, in fixed tables, the inferred contracts, conflicts with existing contracts,
   **contract violations** it found (with shrunk counterexamples) and **conflicts with existing tests**.

It never weakens an existing contract or edits an existing test to make something pass – those decisions are yours.
Copy the skill folder (and the ContractCheck projects or packages) into your repository to use it there.

## Limitations

- **Interfaces only.** `Dbc.Make` creates interface proxies; classes, static members and constructors can't carry
  contracts (constructor rules become invariants, which `Dbc.Make` checks). Calls that bypass the proxy aren't checked.
- **Reflection-based.** `DispatchProxy` generates code at run time, so Native AOT is not supported; each checked call
  costs a reflection dispatch plus the compiled clauses. Source-generated proxies are planned
  ([spec](docs/specs/quartz-willow-x7kgtw.md)).
- **Process-wide checking level.** One level for all proxies; `[ContractOption]` switches single interfaces or
  members off.
- **One contract per interface, not per method body.** Contracts describe the interface every implementation shares.

## Migrating from ContractExpressions and ContractExpressions4

ContractCheck replaces both earlier projects of this repository. Rename the namespace (`ContractExpressions` /
`ContractExpressions4` → `ContractCheck`, `ContractExpressions4.Check` → `ContractCheck.FsCheck`); `Dbc.Def` and
`Dbc.Make` keep their shape. Behaviour changes, decided with CodeContracts as the tie-breaker
([spec](docs/specs/amber-harbor-v9p6ri.md), [ADRs](docs/adr)):

| # | Topic | Now |
|---|---|---|
| R1–R2 | Failures | `Contract.ContractFailed` is raised (`SetHandled` works), then the public `ContractViolationException`; kinds are `ContractFailureKind` (CE4's `ContractKind` is gone) |
| R3 | `Requires<TException>` | always throws `TException`, legacy constructor rules (CE4 ignored `TException`) |
| R4 | Messages | .NET format with the printed condition and the member name |
| R5 | `OldValue` | any expression, several per clause, per index in quantifiers |
| R6–R7 | `Result` / `OldValue` misuse | definition-time errors instead of `default` values or `InvalidCastException` |
| R8 | Interface invariants | checked at creation and after non-pure members; not on entry (CE4), no longer ignored (CE) |
| R9 | `Invariant` in a member `Def` | checked after that member only |
| R10–R11 | `Assert`, `Assume`, `EnsuresOnThrow` | supported (CE4 rejected them); `EnsuresOnThrow<T>` only for `T` (CE checked any exception) |
| R12 | Order | pre → old → call → post → invariants |
| R13–R14 | Selectors, scoping | outermost member call, validated; contracts per interface plus base interfaces, no leaks between siblings |
| R15–R17 | Loading, empty `Def`, `Make(null)` | contract class runs once, validated; empty `Def` is a no-op; `ArgumentNullException` |
| R18–R21 | Target exceptions, arity, async, speed | stack traces kept; 0–15 parameters + `DefSetter`; async members; compiled clauses |

## Repository layout

| Path | Contents |
|---|---|
| `ContractCheck/` | The library; `Features/Definitions`, `Features/Enforcement`, `Features/Failures` ([index](docs/features.md)) |
| `ContractCheck.FsCheck/` | `DbcPropertyTest` for FsCheck |
| `ContractCheck.DependencyInjection/` | `AddContractChecks` for Microsoft.Extensions.DependencyInjection |
| `ContractCheck.Analyzers/`, `ContractCheck.CodeFixes/` | The Roslyn analyzer and its code fix, packed into the ContractCheck package |
| `ContractCheck.Samples/` | The CodeContracts and ContractExpressions4 demo programs, ported to ContractCheck |
| `ContractCheck.Tests/` | xUnit v3 suite: the API, the analyzer, the legacy test ports, both former projects' suites |
| `ContractCheck.Samples.PropertyTests/` | Property tests of the samples, created by the skill's init script |
| `CodeContracts/` | Submodule: Microsoft's original CodeContracts, the reference for semantics |
| `docs/` | Specs, ADRs, feature index |
| `.claude/` | Claude Code skills and hooks; `.github/` has the GitHub Copilot equivalents and the workflows |

## Building and testing

Requires the .NET 10 SDK (`global.json`); the libraries target .NET 8, 9 and 10. A dev container is included.

```
git submodule update --init                                     # optional: the CodeContracts reference sources
dotnet build ContractExpressions.sln
dotnet test ContractExpressions.sln                             # tests on .NET 10
dotnet test ContractExpressions.sln -p:TestAllFrameworks=true   # on .NET 8, 9 and 10 (needs those runtimes), as CI does
```

[CI](.github/workflows/ci.yml) builds and tests every pull request in Debug and Release on .NET 8, 9 and 10 and packs the
packages. Pushing a tag `vX.Y.Z` publishes `ContractCheck`, `ContractCheck.FsCheck` and
`ContractCheck.DependencyInjection` X.Y.Z to nuget.org ([release workflow](.github/workflows/release.yml), repository
secret `NUGET_API_KEY`).

## Contributing

Issues and pull requests are welcome. Non-trivial changes start with a spec in [`docs/specs`](docs/specs); design
decisions are recorded as ADRs in [`docs/adr`](docs/adr); every public behaviour has a test in `ContractCheck.Tests`.
Commits follow [Conventional Commits](https://www.conventionalcommits.org/). See [`CLAUDE.md`](CLAUDE.md) for the
workflow the repository's agents follow.

## License

[MIT](LICENSE) © jk09
